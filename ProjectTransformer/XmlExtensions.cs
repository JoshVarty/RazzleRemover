using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ProjectTransformer
{
    public static class XmlExtensions
    {
        public static string GetValue(this XElement element, string name)
        {
            return element.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value;
        }
    }
}
