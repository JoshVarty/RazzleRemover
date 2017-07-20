using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ProjectTransformer
{
    public static class ExtensionMethods
    {
        public static string GetValue(this XElement element, string name)
        {
            return element.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value;
        }

        public static void AppendPropertyIfSet(this StringBuilder sb, string propertyValue, string propertyName)
        {
            if (!String.IsNullOrWhiteSpace(propertyValue))
            {
                sb.AppendLine($"    <{propertyName}>{propertyValue}</{propertyName}>");
            }
        }
    }
}
