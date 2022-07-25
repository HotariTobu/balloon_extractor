using SharedWPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace dataset_generator
{
    public class PageImage : Control
    {
        static PageImage()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PageImage), new FrameworkPropertyMetadata(typeof(PageImage)));
        }

        private List<Int32Rect> Rects = new List<Int32Rect>();

        #region == SourcePath ==

        public static readonly DependencyProperty SourcePathProperty = DependencyProperty.Register(nameof(SourcePath), typeof(string), typeof(PageImage), new FrameworkPropertyMetadata("",
          (d, e) => ((PageImage)d).OnSourcePathChanged((string)e.OldValue, (string)e.NewValue)));
        public string SourcePath { get => (string)GetValue(SourcePathProperty); set => SetValue(SourcePathProperty, value); }

        protected virtual void OnSourcePathChanged(string oldPath, string newPath)
        {
            if (oldPath != null && MaskSource != null && IsMaskSourceUpdated)
            {
                /*string? oldMaskPath = Exporter.GetMaskPath(oldPath);
                if (oldMaskPath != null)
                {
                    Exporter.SaveBitmap(MaskSource, oldMaskPath);
                }*/
                if (PageSource != null && MaskBits2d != null)
                {
                    Exporter.Extract(PageSource, MaskBits2d, Rects, oldPath);
                    Rects.Clear();
                }
            }

            /*TransformedBitmap? transformedBitmap = Importer.ImportBitmap(newPath);
            if (transformedBitmap == null)
            {
                return;
            }*/

            BitmapImage? bitmapImage = Importer.LoadBitmap(newPath);
            if (bitmapImage == null)
            {
                return;
            }
            FormatConvertedBitmap transformedBitmap = new FormatConvertedBitmap(bitmapImage, PixelFormats.Gray8, BitmapPalettes.Gray256, 0);

            byte[] pixels = new byte[transformedBitmap.PixelWidth * transformedBitmap.PixelHeight];
            int stride = transformedBitmap.PixelWidth * transformedBitmap.Format.BitsPerPixel / 8;
            transformedBitmap.CopyPixels(pixels, stride, 0);

            PixelWidth = transformedBitmap.PixelWidth + 2;
            PixelHeight = transformedBitmap.PixelHeight + 2;

            WriteableBitmap pageSource = new WriteableBitmap(PixelWidth, PixelHeight, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256);
            byte[] newPixels = new byte[PixelWidth * PixelHeight];
            int newStride = stride + 2;
            for (int y = 0; y < transformedBitmap.PixelHeight; y++)
            {
                int sourceIndex = y * stride;
                int destinationIndex = (y + 1) * newStride + 1;
                Array.Copy(pixels, sourceIndex, newPixels, destinationIndex, stride);
            }
            pageSource.WritePixels(new Int32Rect(0, 0, PixelWidth, PixelHeight), newPixels, newStride, 0);
            PageSource = pageSource;

            WriteableBitmap? maskSource = null;

            string? newMaskPath = Exporter.GetMaskPath(newPath);
            if (newMaskPath != null && File.Exists(newMaskPath))
            {
                BitmapImage? maskImage = Importer.LoadBitmap(newMaskPath);
                if (maskImage != null)
                {
                    maskSource = new WriteableBitmap(maskImage);
                }
            }

            if (maskSource == null || maskSource.PixelWidth != PixelWidth || maskSource.PixelHeight != PixelHeight || maskSource.Format.BitsPerPixel != 1)
            {
                maskSource = new WriteableBitmap(PixelWidth, PixelHeight, 96, 96, PixelFormats.Indexed1, new BitmapPalette(new Color[] { Colors.Transparent, Colors.Red, }));
            }
                
            MaskSource = maskSource;
            IsMaskSourceUpdated = false;
            MaskBits2d = new Bits2d(maskSource.BackBuffer, maskSource.BackBufferStride, PixelHeight);
        }

        #endregion

        private int PixelWidth;
        private int PixelHeight;

        #region == PageSource ==

        private static readonly DependencyPropertyKey PageSourcePropertyKey = DependencyProperty.RegisterReadOnly(nameof(PageSource), typeof(WriteableBitmap), typeof(PageImage), new FrameworkPropertyMetadata());
        public static readonly DependencyProperty PageSourceProperty = PageSourcePropertyKey.DependencyProperty;
        public WriteableBitmap? PageSource { get => (WriteableBitmap)GetValue(PageSourceProperty); private set => SetValue(PageSourcePropertyKey, value); }

        #endregion
        #region == MaskSource ==

        private static readonly DependencyPropertyKey MaskSourcePropertyKey = DependencyProperty.RegisterReadOnly(nameof(MaskSource), typeof(WriteableBitmap), typeof(PageImage), new FrameworkPropertyMetadata());
        public static readonly DependencyProperty MaskSourceProperty = MaskSourcePropertyKey.DependencyProperty;
        public WriteableBitmap? MaskSource { get => (WriteableBitmap)GetValue(MaskSourceProperty); private set => SetValue(MaskSourcePropertyKey, value); }

        private bool IsMaskSourceUpdated;
        private Bits2d? MaskBits2d;

        private void UpdateMaskSource(Func<WriteableBitmap, WriteableBitmap, Bits2d, Int32Rect> func)
        {
            if (PageSource == null || MaskSource == null || MaskBits2d == null)
            {
                return;
            }

            MaskSource.Lock();
            MaskSource.AddDirtyRect(func(PageSource, MaskSource, MaskBits2d));
            MaskSource.Unlock();

            IsMaskSourceUpdated = true;
        }

        #endregion

        #region == Threshold ==

        public static readonly DependencyProperty ThresholdProperty = DependencyProperty.Register(nameof(Threshold), typeof(int), typeof(PageImage), new FrameworkPropertyMetadata());
        public int Threshold { get => (int)GetValue(ThresholdProperty); set => SetValue(ThresholdProperty, value); }

        #endregion
        #region == Fill ==

        unsafe private void FillMask(int pointX, int pointY)
        {
            UpdateMaskSource((pageSpurce, maskSource, maskBits2d) =>
            {
                byte* bitmapPointer = (byte*)pageSpurce.BackBuffer.ToPointer();

                bool isMasked = maskBits2d.GetBit(pointX, pointY);

                int threshold = Threshold;
                Int32Rect rect = BitmapUtils.Fill(pointX, pointY,
                        (x, y) => (*(bitmapPointer + pageSpurce.GetIndexAt(x, y))) < threshold,
                        (x, y) => maskBits2d.GetBit(x, y) != isMasked,
                        maskBits2d.SetBits);

                if (!rect.HasArea)
                {
                    return rect;
                }

                int margin = 2;

                int width = (rect.Width / 8 + 2) + (margin / 8 + 1) * 2;
                int height = rect.Height + margin * 2;

                byte* buffer = stackalloc byte[width * height];
                Bits2d bitmask = new Bits2d(buffer, width, height);

                Int32Point point = new Int32Point(margin + 8, margin);

                maskBits2d.CopyTo(bitmask, rect.Point(), point, rect.Size());

                byte* pointer1 = buffer;
                byte* pointer2 = buffer + (height - 1) * width;

                for (int x = 0; x < width; x++)
                {
                    *pointer1 = *pointer2 = 0xFF;
                    pointer1 += 1;
                    pointer2 += 1;
                }

                pointer1 = buffer;
                pointer2 = buffer + width - 1;

                for (int y = 0; y < height; y++)
                {
                    *pointer1 |= 128;
                    *pointer2 |= 1;
                    pointer1 += width;
                    pointer2 += width;
                }

                BitmapUtils.Fill(1, 1, bitmask.GetBit, bitmask.GetBit, bitmask.SetBits);

                bitmask.CopyTo(maskBits2d, point, rect.Point(), rect.Size(), (src, dst) => (byte)(src ^ dst ^ 0xFF));
                Rects.Add(rect);
                return rect;
            });
        }

        #endregion

        #region == Mouse Events ==

        private Point LastPagePoint;
        private bool LastPointMask;

        private Point ConvertPoint(Point point)
        {
            return new Point
            {
                X = point.X * PixelWidth / ActualWidth,
                Y = point.Y * PixelHeight / ActualHeight
            };
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            Point mousePoint = e.GetPosition(this);
            Point pagePoint = ConvertPoint(mousePoint);

            int pointX = (int)pagePoint.X;
            int pointY = (int)pagePoint.Y;

            if (e.ChangedButton == MouseButton.Right)
            {
                UpdateMaskSource((pageSpurce, maskSource, maskBits2d) =>
                {
                    LastPointMask = !maskBits2d.GetBit(pointX, pointY);
                    maskBits2d.SetBit(pointX, pointY);
                    return new Int32Rect(pointX, pointY, 1, 1);
                });
            }

            LastPagePoint = pagePoint;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Point mousePoint = e.GetPosition(this);
            Point pagePoint = ConvertPoint(mousePoint);

            if (e.RightButton == MouseButtonState.Pressed)
            {
                UpdateMaskSource((pageSpurce, maskSource, maskBits2d) =>
                {
                    return BitmapUtils.DrawLine(LastPagePoint, pagePoint, (leftX, rightX, y) => maskBits2d.SetBits(leftX, rightX, y, LastPointMask));
                });
            }

            LastPagePoint = pagePoint;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            Point mousePoint = e.GetPosition(this);
            Point pagePoint = ConvertPoint(mousePoint);

            if (e.ChangedButton == MouseButton.Left)
            {
                FillMask((int)pagePoint.X, (int)pagePoint.Y);
            }
        }

        #endregion
    }
}
