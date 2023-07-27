using RelativeBrushes;
using System.Windows;
using System.Windows.Media;

namespace WpfDemoApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ChangeColor_OnClick(object sender, RoutedEventArgs e)
        {
            Props.SetContentBrush(ButtonImage1, Brushes.Yellow);
            Button2.Foreground = Brushes.Yellow;
        }

        private void ChangeMiddleColor_OnClick(object sender, RoutedEventArgs e)
        {
            BrushCollection[2] = Brushes.Green;
        }

        private void BtnChangeMiddleColors_OnClick(object sender, RoutedEventArgs e)
        {
            //Many icons have same Color (application wide)
            if (FindResource("BrushCollectionRes") is BrushCollection brushes)
            {
                brushes[2] = Brushes.Green;
            }
        }
    }
}
