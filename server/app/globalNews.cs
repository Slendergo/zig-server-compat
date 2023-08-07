using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Anna.Request;
using common;
using common.resources;

namespace server.app
{
    class globalNews : RequestHandler
    {
        private static byte[] _data;

        public override void InitHandler(Resources resources)
        {
            string data = Utils.Read(resources.ResourcePath + "/data/globalNews.json");
            _data = Encoding.UTF8.GetBytes(data);
        }

        public override void HandleRequest(RequestContext context, NameValueCollection query)
        {
            Write(context, _data);
        }
    }
}
