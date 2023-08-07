using System.Collections.Specialized;
using System.Threading.Tasks;
using Anna.Request;
using common;
using common.resources;
using NLog;
using StackExchange.Redis;

namespace server.account
{
    class purchaseCharSlot : RequestHandler
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public override void HandleRequest(RequestContext context, NameValueCollection query)
        {
            DbAccount acc;
            var status = Database.Verify(query["guid"], query["password"], out acc);
            if (status == LoginStatus.OK)
            {
                using (var l = Database.Lock(acc))
                {
                    if (!Database.LockOk(l))
                    {
                        Write(context, "<Error>Account in use</Error>");
                        return;
                    }

                    var currency = CurrencyType.Gold;
                    var price = Program.Resources.Settings.NewAccounts.SlotCost;

                    if (currency == CurrencyType.Gold && acc.Credits < price ||
                        currency == CurrencyType.Fame && acc.Fame < price)
                    {
                        Write(context, "<Error>Insufficient funds</Error>");
                        return;
                    }

                    var trans = Database.Conn.CreateTransaction();
                    var t1 = Database.UpdateCurrency(acc, -price, currency, trans);
                    trans.AddCondition(Condition.HashEqual(acc.Key, "maxCharSlot", acc.MaxCharSlot));
                    trans.HashIncrementAsync(acc.Key, "maxCharSlot");
                    var t2 = trans.ExecuteAsync();

                    Task.WhenAll(t1, t2).ContinueWith(r =>
                    {
                        if (t2.IsCanceled || !t2.Result)
                        {
                            Write(context, "<Error>Internal Server Error</Error>");
                            return;
                        }

                        acc.MaxCharSlot++;
                        Write(context, "<Success />");
                    }).ContinueWith(e =>
                        Log.Error(e.Exception.InnerException.ToString()),
                        TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            else
                Write(context, "<Error>" + status.GetInfo() + "</Error>");
        }
    }
}
