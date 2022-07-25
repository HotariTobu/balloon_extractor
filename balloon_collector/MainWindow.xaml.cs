using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace balloon_collector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MWVM VM;
        private Queue<string> SourcePaths = new Queue<string>();

        public MainWindow()
        {
            InitializeComponent();

            VM = (MWVM)DataContext;
        }

        private static readonly IEnumerable<byte[]> Separator = Enumerable.Repeat(new byte[1], 4);

        private void PatchUp()
        {
            Directory.CreateDirectory(VM.ToPath);
            int index = 0;

            int count = SourcePaths.Count;
            while (count > 0)
            {
                int maxWidth = 0;
                List<IEnumerable<byte>> lines = new List<IEnumerable<byte>>();

                int length = Math.Min(500, count);
                for (int i = 0; i < length; i++)
                {
                    string sourcePath = SourcePaths.Dequeue();

                    BitmapSource? bitmap = ImportBitmap(sourcePath);
                    if (bitmap == null)
                    {
                        continue;
                    }

                    int width = bitmap.PixelWidth;
                    int height = bitmap.PixelHeight;

                    if (maxWidth < width)
                    {
                        maxWidth = width;
                    }

                    byte[] pixels = new byte[width * height];
                    bitmap.CopyPixels(pixels, width, 0);

                    lines.AddRange(pixels.Chunk(width));
                    lines.AddRange(Separator);
                }
                count -= length;

                int resultWidth = maxWidth;
                int resultHeight = lines.Count;

                WriteableBitmap resultBitmap = new WriteableBitmap(resultWidth, resultHeight, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256);

                byte[] resultPixels = lines
                     .Select(x =>
                     {
                         int count = x.Count();
                         return x.Concat(Enumerable.Repeat((byte)(count > 1 ? 255 : 0), resultWidth - count));
                     })
                     .SelectMany(x => x)
                     .ToArray();

                resultBitmap.WritePixels(new Int32Rect(0, 0, resultWidth, resultHeight), resultPixels, resultWidth, 0);

                string path = Path.Combine(VM.ToPath, $"{index}.png");
                SaveBitmap(resultBitmap, path);
            }
        }

        private void CollectFilePaths(IEnumerable<string> paths)
        {
            foreach (string path in paths.OrderBy(selector))
            {
                if (File.Exists(path))
                {
                    SourcePaths.Enqueue(path);
                }
                else if (Directory.Exists(path))
                {
                    CollectFilePaths(Directory.EnumerateFileSystemEntries(path));
                }
            }

            string selector(string path)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                return new string('0', 16 - name.Length) + name;
            }
        }

        #region == Drag And Drop ==

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                CollectFilePaths((string[])e.Data.GetData(DataFormats.FileDrop));
                PatchUp();
            }
        }

        #endregion

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

        public static FormatConvertedBitmap? ImportBitmap(string path)
        {
            BitmapImage? bitmapImage = LoadBitmap(path);
            if (bitmapImage == null)
            {
                return null;
            }

            return new FormatConvertedBitmap(bitmapImage, PixelFormats.Gray8, BitmapPalettes.Gray256, 0);
        }

        public static void SaveBitmap(BitmapSource bitmapSource, string path)
        {
            PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
            pngBitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            using FileStream stream = new FileStream(path, FileMode.Create);
            pngBitmapEncoder.Save(stream);
        }
    }
}
