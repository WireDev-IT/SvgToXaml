using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Windows;

namespace SvgToXaml.TextViewer
{
    public class XmlViewer : TextEditor
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(XmlViewer), new PropertyMetadata(default(string), TextChanged));

        private static new void TextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            XmlViewer xmlViewer = (XmlViewer)dependencyObject;
            xmlViewer.Document.Text = (string)args.NewValue;
        }

        public new string Text
        {
            get => Document.Text;
            set => SetValue(TextProperty, value);
        }

        public XmlViewer()
        {
            SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
            //Options.AllowScrollBelowDocument = true;
            Options.EnableHyperlinks = true;
            Options.EnableEmailHyperlinks = true;
            //Options.ShowSpaces = true;
            //Options.ShowTabs = true;
            //Options.ShowEndOfLine = true;              

            ShowLineNumbers = true;

            //_foldingManager = FoldingManager.Install(TextArea);
            //_foldingStrategy = new XmlFoldingStrategy();
            //Document.TextChanged += DocumentTextChanged;
        }

        //der ganze Folding Quatsch funktioniert nicht richtig -> bleiben lassen
        //private XmlFoldingStrategy _foldingStrategy;
        //private FoldingManager _foldingManager;
        //private volatile bool _updateFoldingRequested;
        //private async void DocumentTextChanged(object sender, EventArgs eventArgs)
        //{
        //    if (!_updateFoldingRequested)
        //    {
        //        _updateFoldingRequested = true;
        //        await Task.Delay(1000);
        //    }
        //    _updateFoldingRequested = false;
        //    _foldingStrategy.UpdateFoldings(_foldingManager, Document);
        //}

    }
}
