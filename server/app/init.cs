using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Anna.Request;
using common.resources;

namespace server.app
{
    class init : RequestHandler
    {
        private static byte[] _data;

        public override void InitHandler(Resources resources)
        {
            XElement root = new XElement(Program.Resources.Settings.Xml);
            //root.Add(new XElement("SkinsList", XElement.Parse(File.ReadAllText(resources.ResourcePath + "/xml/Skins.xml"))));
            _data = Encoding.UTF8.GetBytes(root.ToString());
        }

        public override void HandleRequest(RequestContext context, NameValueCollection query)
        {
            Write(context, _data);
        }
    }
}
