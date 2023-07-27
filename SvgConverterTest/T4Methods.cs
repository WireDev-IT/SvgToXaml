using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SvgConverterTest
{
    public class T4Methods
    {
        [Test]
        public void Test_KeysFromXaml()
        {
            IEnumerable<string> keys = KeysFromXaml(@"TestFiles\Expected\SvgDirToXamlTest_withUseCompResKey.xaml", out string nameSpaceName, out string prefix);
            Console.WriteLine($"NS:{nameSpaceName}, Prefix:{prefix}");
            foreach (string key in keys)
            {
                Console.WriteLine(key);
            }
        }

        public static IEnumerable<string> KeysFromXaml(string fileName, out string nameSpace, out string prefix)
        {
            XDocument doc = XDocument.Load(fileName);
            //var allElems = doc.Root.Elements(); //doc.Descendants(); das wären alle samt SubNodes
            XNamespace xamlNs = "http://schemas.microsoft.com/winfx/2006/xaml";
            //var keyAttrs = allElems.Attributes(xamlNs+"Key");


            //Console.WriteLine(keyAttrs.Count());
            //foreach (var attr in keyAttrs)
            //{
            //    Console.WriteLine(attr.Name);
            //}
            nameSpace = doc.Root.LastAttribute.Value; //hoffentlich ist es immer das letzte, aber nach Namen suchen is nich, und andere ausschließen ist auch nicht besser
            string[] keys = doc.Root.Elements().Attributes(xamlNs + "Key").Select(a => a.Value).ToArray();
            //keys liegen in dieser Form vor: { x: Static NameSpaceName:XamlName.Color1}

            prefix = "unknownPrefix";
            string first = keys.FirstOrDefault();
            if (first != null)
            {
                int p1 = first.LastIndexOf(":");
                int p2 = first.LastIndexOf("}");
                if (p1 < p2)
                {
                    prefix = first.Substring(p1 + 1, p2 - p1 - 1).Split('.').FirstOrDefault();
                }
            }

            string[] names = keys.Select(key =>
            {
                int p1 = key.LastIndexOf(".");
                int p2 = key.LastIndexOf("}");
                return p1 < p2 ? key.Substring(p1 + 1, p2 - p1 - 1) : key;
            }).ToArray();


            return names;
        }
    }
}
