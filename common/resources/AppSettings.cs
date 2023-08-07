using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace common.resources
{
    public class AppSettings
    {
        public readonly XElement Xml;

        public readonly int UseExternalPayments;
        public readonly int MaxStackablePotions;
        public readonly NewAccounts NewAccounts;
        public readonly NewCharacters NewCharacters;

        public AppSettings(string dir)
        {
            XElement e = XElement.Parse(Utils.Read(dir));
            Xml = e;
            UseExternalPayments = e.GetValue<int>("UseExternalPayments");
            MaxStackablePotions = e.GetValue<int>("MaxStackablePotions");

            var newAccounts = e.Element("NewAccounts");
            NewAccounts = new NewAccounts(e.Element("NewAccounts"));
            newAccounts.Remove(); // don't export with /app/init

            var newCharacters = e.Element("NewCharacters");
            NewCharacters = new NewCharacters(e.Element("NewCharacters"));
            newCharacters.Remove();
        }
    }

    public class NewAccounts
    {
        public readonly int MaxCharSlot;
        public readonly int VaultCount;
        public readonly int Fame;
        public readonly int Credits;
        public readonly int SlotCost;
        public readonly CurrencyType SlotCurrency;
        public readonly bool ClassesUnlocked;
        public readonly bool SkinsUnlocked;

        public NewAccounts(XElement e)
        {
            MaxCharSlot = e.GetValue<int>("MaxCharSlot", 1);
            VaultCount = e.GetValue<int>("VaultCount", 1);
            Fame = e.GetValue<int>("Fame", 0);
            Credits = e.GetValue<int>("Credits", 0);

            ClassesUnlocked = e.HasElement("ClassesUnlocked");
            SkinsUnlocked = e.HasElement("SkinsUnlocked");

            SlotCost = e.GetValue<int>("SlotCost", 1000);
            SlotCurrency = (CurrencyType)e.GetValue<int>("SlotCurrency", 0);
            if (SlotCurrency != CurrencyType.Fame && SlotCurrency != CurrencyType.Gold)
                SlotCurrency = CurrencyType.Gold;
        }
    }

    public class NewCharacters
    {
        public readonly bool Maxed;
        public readonly int Level;

        public NewCharacters(XElement e)
        {
            Maxed = e.HasElement("Maxed");
            Level = e.GetValue<int>("Level", 1);
        }
    }
}
