using FluentAssertions;
using NUnit.Framework;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using SvgConverter;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SvgConverterTest
{
    [TestFixture]
    public class ConverterTests
    {
        private void CheckXamlOutput(string xaml, string testSuffix, bool check = true, [CallerMemberName] string testName = null)
        {
            CheckXamlOutput(xaml, check, testName, testSuffix);
        }

        private void CheckXamlOutput(string xaml, bool check = true, [CallerMemberName] string testName = null, string testSuffix = null)
        {
            Console.WriteLine(xaml);
            _ = Directory.CreateDirectory("TestFiles\\Actual");
            string filename = testName;
            if (testSuffix != null)
            {
                filename += $"_{testSuffix}";
            }

            filename += ".xaml";
            File.WriteAllText($"TestFiles\\Actual\\{filename}", xaml);
            if (check)
            {
                string expected = File.ReadAllText($"TestFiles\\Expected\\{filename}");
                _ = xaml.Should().Be(expected);
            }
        }

        [TestCase("TestFiles\\cloud-3-icon.svg")]
        [TestCase("TestFiles\\3d-view-icon.svg")]
        [TestCase("TestFiles\\JOG.svg")]
        public void ConvertFileToDrawingGroup(string filename)
        {
            WpfDrawingSettings settings = new WpfDrawingSettings
            {
                IncludeRuntime = false,
                TextAsGeometry = false,
                OptimizePath = true,
            };
            ResKeyInfo resKeyInfo = new ResKeyInfo { Prefix = "Prefix" };
            string xaml = ConverterLogic.SvgFileToXaml(filename, ResultMode.DrawingGroup, resKeyInfo, false, settings);

            CheckXamlOutput(xaml, Path.GetFileName(filename));
        }

        [Test]
        public void ConvertFileToDrawingGroup2()
        {
            WpfDrawingSettings settings = new WpfDrawingSettings
            {
                IncludeRuntime = false,
                TextAsGeometry = false,
                OptimizePath = true,
            };
            ResKeyInfo resKeyInfo = new ResKeyInfo { Prefix = "Prefix" };
            string xaml = ConverterLogic.SvgFileToXaml("TestFiles\\3d-view-icon.svg", ResultMode.DrawingGroup, resKeyInfo, false, settings);
            CheckXamlOutput(xaml);
        }

        [TestCase("TestFiles\\cloud-3-icon.svg")]
        [TestCase("TestFiles\\3d-view-icon.svg")]
        [TestCase("TestFiles\\JOG.svg")]
        public void ConvertFileToDrawingImage(string filename)
        {
            WpfDrawingSettings settings = new WpfDrawingSettings
            {
                IncludeRuntime = false,
                TextAsGeometry = false,
                OptimizePath = true,
            };
            ResKeyInfo resKeyInfo = new ResKeyInfo { Prefix = "Prefix" };
            string xaml = ConverterLogic.SvgFileToXaml(filename, ResultMode.DrawingImage, resKeyInfo, true, settings);
            CheckXamlOutput(xaml, Path.GetFileName(filename));
        }

        [TestCase("TestFiles\\cloud-3-icon.svg")]
        [TestCase("TestFiles\\JOG.svg")]
        public void ConvertFileToDrawingGroupWithRuntime(string filename)
        {
            WpfDrawingSettings settings = new WpfDrawingSettings
            {
                IncludeRuntime = true,
                TextAsGeometry = false,
                OptimizePath = true,
            };
            ResKeyInfo resKeyInfo = new ResKeyInfo { Prefix = "Prefix" };
            string xaml = ConverterLogic.SvgFileToXaml(filename, ResultMode.DrawingGroup, resKeyInfo, false, settings);
            CheckXamlOutput(xaml, Path.GetFileName(filename));
        }

        [Test]
        public void SvgDirToXamlTest_withNamePrefix()
        {
            WpfDrawingSettings settings = new WpfDrawingSettings
            {
                IncludeRuntime = false,
                TextAsGeometry = false,
                OptimizePath = true,
            };
            ResKeyInfo resKeyInfo = new ResKeyInfo { XamlName = "Test", Prefix = "NamePrefix" };
            string xaml = ConverterLogic.SvgDirToXaml("TestFiles\\", resKeyInfo, settings, true);
            CheckXamlOutput(xaml);
        }

        [Test]
        public void SvgDirToXamlTest_withUseCompResKey()
        {
            WpfDrawingSettings settings = new WpfDrawingSettings
            {
                IncludeRuntime = false,
                TextAsGeometry = false,
                OptimizePath = true,
            };

            string xaml = ConverterLogic.SvgDirToXaml("TestFiles\\", ResKeyInfoUseCompResKey, settings, false);
            CheckXamlOutput(xaml);
        }

        [Test]
        public void SvgDirToXaml_with_defaultSettingsTest()
        {
            ResKeyInfo resKeyInfo = new ResKeyInfo { XamlName = "Test", Prefix = "NamePrefix" };
            string xaml = ConverterLogic.SvgDirToXaml("TestFiles\\", resKeyInfo, null, false);
            CheckXamlOutput(xaml);
        }

        [Test, STAThread]
        public void Handwheel() //Full integrated with all optimizations
        {
            ResKeyInfo resKeyInfo = new ResKeyInfo { Prefix = "Prefix" };
            string xaml = ConverterLogic.SvgFileToXaml("TestFiles\\Handwheel.svg", ResultMode.DrawingGroup, resKeyInfo, false, null);
            CheckXamlOutput(xaml);
        }
        [Test, STAThread]
        public void Handwheel1() //pure svg# without any modifications
        {
            FileSvgReader fileReader = new FileSvgReader(null);
            DrawingGroup drawing = fileReader.Read("TestFiles\\Handwheel.svg");
            XmlXamlWriter writer = new XmlXamlWriter(null);
            string xaml = writer.Save(drawing);
            CheckXamlOutput(xaml);
        }
        [Test, STAThread]
        public void Handwheel2() //integrated conversion, manual writing
        {
            DrawingGroup drawing = SvgConverter.ConverterLogic.SvgFileToWpfObject("TestFiles\\Handwheel.svg", null);
            XmlXamlWriter writer = new XmlXamlWriter(null);
            string xaml = writer.Save(drawing);
            CheckXamlOutput(xaml);
        }
        [Test, STAThread]
        public void Handwheel3() //integrated conversion, integrated writing
        {
            DrawingGroup drawing = ConverterLogic.SvgFileToWpfObject("TestFiles\\Handwheel.svg", null);
            string xaml = ConverterLogic.SvgObjectToXaml(drawing, true, "Test", false);
            CheckXamlOutput(xaml);
        }

        private static ResKeyInfo ResKeyInfoUseNamePrefix
        {
            get
            {
                ResKeyInfo resKeyInfo = new ResKeyInfo
                {
                    UseComponentResKeys = false,
                    Prefix = "NamePrefix"
                };
                return resKeyInfo;
            }
        }

        private static ResKeyInfo ResKeyInfoUseCompResKey
        {
            get
            {
                ResKeyInfo resKeyInfo = new ResKeyInfo
                {
                    UseComponentResKeys = true,
                    XamlName = "XamlName",
                    NameSpaceName = "NameSpaceName",
                    NameSpace = "MyLib.Components.Images",
                    //Prefix = "NamePrefix"
                };
                return resKeyInfo;
            }
        }

        [Test]
        public void BuildDrawingGroupName_returns_simpleName()
        {
            ResKeyInfo resKeyInfo = new ResKeyInfo
            {
                UseComponentResKeys = false,
                Prefix = null
            };
            _ = ConverterLogic.BuildDrawingGroupName("ElementName", resKeyInfo).Should().Be("ElementNameDrawingGroup");
        }
        [Test]
        public void BuildDrawingGroupName_returns_prefixedName()
        {
            ResKeyInfo resKeyInfo = new ResKeyInfo
            {
                UseComponentResKeys = false,
                Prefix = "NamePrefix"
            };
            _ = ConverterLogic.BuildDrawingGroupName("ElementName", resKeyInfo).Should().Be("NamePrefix_ElementNameDrawingGroup");
        }
        [Test]
        public void BuildDrawingGroupName_returns_prefixedName_using_CompResKey()
        {
            ResKeyInfo resKeyInfo = new ResKeyInfo
            {
                UseComponentResKeys = true,
                XamlName = "XamlName",
                NameSpaceName = "NameSpaceName",
                //Prefix = "NamePrefix"
            };
            string key = ConverterLogic.BuildDrawingGroupName("ElementName", resKeyInfo);
            Console.WriteLine(key);
            _ = key.Should().Be("{x:Static NameSpaceName:XamlName.ElementNameDrawingGroupKey}");
        }
        [Test]
        public void BuildDrawingImageName_returns_simpleName()
        {
            ResKeyInfo resKeyInfo = new ResKeyInfo
            {
                UseComponentResKeys = false,
                Prefix = null
            };
            _ = ConverterLogic.BuildDrawingImageName("ElementName", resKeyInfo).Should().Be("ElementNameDrawingImage");
        }
        [Test]
        public void BuildDrawingImageName_returns_prefixedName()
        {
            _ = ConverterLogic.BuildDrawingImageName("ElementName", ResKeyInfoUseNamePrefix).Should().Be("NamePrefix_ElementNameDrawingImage");
        }

        [Test]
        public void BuildDrawingImageName_returns_prefixedName_using_CompResKey()
        {
            string key = ConverterLogic.BuildDrawingImageName("ElementName", ResKeyInfoUseCompResKey);
            Console.WriteLine(key);
            _ = key.Should().Be("{x:Static NameSpaceName:XamlName.ElementNameDrawingImageKey}");
        }

        [Test]
        public void BuildResKeyReference_Static()
        {
            string actual = ConverterLogic.BuildResKeyReference("NamePrefix_ElementName", false);
            Console.WriteLine(actual);
            _ = actual.Should().Be("{StaticResource NamePrefix_ElementName}");
        }

        [Test]
        public void BuildResKeyReference_usingCompResKey()
        {
            string actual = ConverterLogic.BuildResKeyReference("{x:Static NameSpaceName:XamlName.ElementName}", true);
            Console.WriteLine(actual);
            _ = actual.Should().Be("{DynamicResource {x:Static NameSpaceName:XamlName.ElementName}}");
        }

        [Test]
        public void GetElemNameFromResKey_NamePrefix()
        {
            _ = ConverterLogic.GetElemNameFromResKey("NamePrefix_ElementName", ResKeyInfoUseNamePrefix).Should().Be("ElementName");
        }
        [Test]
        public void GetElemNameFromResKey_CompResKey()
        {
            _ = ConverterLogic.GetElemNameFromResKey("{x:Static NameSpaceName:XamlName.ElementNameKey}", ResKeyInfoUseCompResKey).Should().Be("ElementName");
        }

        [Test]
        public void GetCorrectClippingElement()
        {
            XDocument doc = XDocument.Load(@"TestFiles\xamlUntidy.xaml");
            ConverterLogic.RemoveResDictEntries(doc.Root);
            System.Collections.Generic.List<XElement> drawingGroupElements = doc.Root.XPathSelectElements("defns:DrawingGroup", ConverterLogic.NsManager).ToList();

            Tuple<XElement, Rect>[] clipElements = drawingGroupElements.Select(dg =>
            {
                XElement element = ConverterLogic.GetClipElement(dg, out Rect rect);
                return Tuple.Create(element, rect);
            }).ToArray();
            foreach (Tuple<XElement, Rect> clipElement in clipElements)
            {
                Console.WriteLine(clipElement.Item2);
                Console.WriteLine(clipElement.Item1);
                Console.WriteLine();
            }

            //clipElements[0].Item2.ShouldBeEquivalentTo(new Rect(0, 0, 40, 40));
            //clipElements[1].Item2.ShouldBeEquivalentTo(new Rect(0, 0, 45, 34));
            //..
        }

    }
}
