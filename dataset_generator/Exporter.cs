using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace dataset_generator
{
    internal static class Exporter
    {
        public static readonly string MaskSuffix = "_mask";
        private static readonly int Radius = 14;
        private static readonly int Margin = Radius * 2;
        private static readonly Int32Point DestinationPoint = new Int32Point(Radius * 8, Radius);
        private static readonly int Diameter = Margin + 1;

        public static void ExportAll(IEnumerable<string> paths, string toPath)
        {
            string[] toPaths = new string[2];
            int[] counts = new int[2];
            for (int i = 0; i < toPaths.Length; i++)
            {
                toPaths[i] = Path.Combine(toPath, i.ToString());
                Directory.CreateDirectory(toPaths[i]);
                counts[i] = Directory.GetFiles(toPaths[i]).Length;
            }

            foreach (string path in paths)
            {
                string? maskPath = GetMaskPath(path);
                if (maskPath == null)
                {
                    continue;
                }

                try
                {
                    TransformedBitmap? bitmapImage = Importer.ImportBitmap(path);
                    BitmapImage? maskImage = Importer.LoadBitmap(maskPath);

                    if (bitmapImage == null || maskImage == null)
                    {
                        continue;
                    }

                    int width = bitmapImage.PixelWidth;
                    int height = bitmapImage.PixelHeight;

                    if (maskImage.PixelWidth != width + 2 || maskImage.PixelHeight != height + 2 || maskImage.Format.BitsPerPixel != 1)
                    {
                        continue;
                    }

                    WriteableBitmap writeableBitmap = new WriteableBitmap(bitmapImage);
                    WriteableBitmap writeableMask = new WriteableBitmap(maskImage);

                    int marginedWidth = width + Margin;
                    int marginedHeight = height + Margin;

                    WriteableBitmap marginedBitmap = new WriteableBitmap(marginedWidth, marginedHeight, 96, 96, bitmapImage.Format, bitmapImage.Palette);

                    Bits2d bitmapBits = new Bits2d(writeableBitmap.BackBuffer, writeableBitmap.BackBufferStride, height);
                    Bits2d marginedBits = new Bits2d(marginedBitmap.BackBuffer, marginedBitmap.BackBufferStride, marginedHeight);

                    bitmapBits.CopyTo(marginedBits, Int32Point.Zero, DestinationPoint, new Int32Size(width * 8, height));

                    unsafe
                    {
                        byte* maskPointer = (byte*)writeableMask.BackBuffer.ToPointer();
                        maskPointer += writeableMask.BackBufferStride;

                        int stride = (writeableMask.PixelWidth + 7) / 8;
                        int lasCount = writeableMask.PixelWidth % 8 - 1;

                        for (int y = 0; y < height; y++)
                        {
                            byte* nextMaskPointer = maskPointer + writeableMask.BackBufferStride;

                            int x = 0;

                            byte b = *maskPointer;
                            maskPointer += 1;

                            for (int j = 0; j < 7; j++)
                            {
                                b <<= 1;
                                save(b, x, y);
                                x++;
                            }

                            for (int i = 2; i < stride; i++)
                            {
                                b = *maskPointer;
                                maskPointer += 1;

                                for (int j = 0; j < 8; j++)
                                {
                                    save(b, x, y);
                                    b <<= 1;
                                    x++;
                                }
                            }

                            b = *maskPointer;
                            maskPointer += 1;

                            for (int j = 0; j < lasCount; j++)
                            {
                                save(b, x, y);
                                b <<= 1;
                                x++;
                            }

                            maskPointer = nextMaskPointer;
                        }
                    }

                    void save(byte b, int x, int y)
                    {
                        int index = (b & 128) != 0 ? 1 : 0;
                        string path = Path.Combine(toPaths[index], $"{counts[index]++}.png");
                        CroppedBitmap croppedBitmap = new CroppedBitmap(marginedBitmap, new Int32Rect(x, y, Diameter, Diameter));
                        SaveBitmap(croppedBitmap, path);
                    }
                }
                catch (Exception e)
                {
                    e.Log();
                }
            }
        }

        public static string? GetMaskPath(string sourcePath)
        {
            string? directoryPath = Path.GetDirectoryName(sourcePath);
            string? fileName = Path.GetFileNameWithoutExtension(sourcePath);
            string? fileExtension = Path.GetExtension(sourcePath);

            if (directoryPath != null && fileName != null && fileExtension != null)
            {
                return Path.Combine(directoryPath, $"{fileName}{MaskSuffix}{fileExtension}");
            }

            return null;
        }

        public static void SaveBitmap(BitmapSource bitmapSource, string path)
        {
            PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
            pngBitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            using FileStream stream = new FileStream(path, FileMode.Create);
            pngBitmapEncoder.Save(stream);
        }

        public static void Extract(WriteableBitmap pageSource, Bits2d maskBits2d, IEnumerable<Int32Rect> rects, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            string directoryPath = Path.GetDirectoryName(path) ?? "";
            string toPath = Path.Combine(directoryPath, "balloons");
            Directory.CreateDirectory(toPath);
            int count = Directory.GetFiles(toPath).Length;

            foreach (Int32Rect rect in rects)
            {
                int width = rect.Width;
                int height = rect.Height;

                WriteableBitmap subBitmap = new WriteableBitmap(new CroppedBitmap(pageSource, rect));
                bool[] subMaskBits = maskBits2d.GetBits(rect);

                unsafe
                {
                    byte* subPointer = (byte*)subBitmap.BackBuffer.ToPointer();
                    fixed (bool* subMaskBitsPointer = subMaskBits)
                    {
                        bool* subMaskPointer = subMaskBitsPointer;


                        for (int y = 0; y < height; y++)
                        {
                            byte* nextSubPointer = subPointer + subBitmap.BackBufferStride;

                            for (int x = 0; x < width; x++)
                            {
                                if (!*subMaskPointer)
                                {
                                    *subPointer = 255;
                                }

                                subPointer += 1;
                                subMaskPointer += 1;
                            }

                            subPointer = nextSubPointer;
                        }
                    }
                }

                string subPath = Path.Combine(toPath, $"{count++}.png");
                SaveBitmap(subBitmap, subPath);
            }
        }
    }
}
