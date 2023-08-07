using Anna.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public static class AnnaExtentions
    {
        public static string ClientIP(this Request r)
        {
            if (r.Headers.ContainsKey("X-Forwarded-For"))
            {
                return r.Headers["X-Forwarded-For"].Last();
            }

            if (r.Headers.ContainsKey("remote_addr"))
            {
                return r.Headers["remote_addr"].Last();
            }

            return r.ListenerRequest.RemoteEndPoint?.Address.ToString();
        }
    }
}
