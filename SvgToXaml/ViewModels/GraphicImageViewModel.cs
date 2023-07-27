using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SvgToXaml.ViewModels
{
    internal class GraphicImageViewModel : ImageBaseViewModel
    {
        public GraphicImageViewModel(string filepath) : base(filepath)
        {
        }
        protected override ImageSource GetImageSource()
        {
            return new BitmapImage(new Uri(Filepath, UriKind.RelativeOrAbsolute));
        }

        public static string SupportedFormats => "*.jpg|*.jpeg|*.png|*.bmp|*.tiff|*.gif";

        protected override string GetSvgDesignInfo()
        {
            return PreviewSource is BitmapImage bi ? $"{bi.PixelWidth}x{bi.PixelHeight}" : null;
        }

        public override bool HasXaml => false;
        public override bool HasSvg => false;
    }
}
