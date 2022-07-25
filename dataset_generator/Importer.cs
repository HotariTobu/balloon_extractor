using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace dataset_generator
{
    internal static class Importer
    {
        private static readonly int UnifiedHeight = 1024;

        public static TransformedBitmap? ImportBitmap(string path)
        {
            BitmapImage? bitmapImage = LoadBitmap(path);
            if (bitmapImage == null)
            {
                return null;
            }

            FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(bitmapImage, PixelFormats.Gray8, BitmapPalettes.Gray256, 0);
            double scale = (UnifiedHeight - 2d) / formatConvertedBitmap.PixelHeight;

            return new TransformedBitmap(formatConvertedBitmap, new ScaleTransform(scale, scale));
        }

        public static BitmapImage? LoadBitmap(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                return null;
            }

            try
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(fileInfo.FullName);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
            catch (Exception e)
            {
                e.Log();
                return null;
            }
        }
    }
}
