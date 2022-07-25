using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace dataset_generator
{
    unsafe internal class Bits2d
    {
        public byte* Pointer { get; }
        public int Width { get; }
        public int Height { get; }

        public Bits2d(byte* pointer, int width, int height)
        {
            Pointer = pointer;
            Width = width;
            Height = height;
        }

        public Bits2d(IntPtr intPtr, int width, int height) : this((byte*)intPtr.ToPointer(), width, height) { }

        public int GetIndexAt(int x, int y)
        {
            return x / 8 + y * Width;
        }

        public int GetIndexAt(Int32Point point) => GetIndexAt(point.X, point.Y);

        public void CopyTo(Bits2d bitmask, Int32Point sourcePoint, Int32Point destinationPoint, Int32Size size, Func<byte, byte, byte>? func = null)
        {
            byte* sourcePointer = Pointer + GetIndexAt(sourcePoint);
            byte* destinationPointer = bitmask.Pointer + bitmask.GetIndexAt(destinationPoint);

            int stride = (sourcePoint.X + size.Width + 7) / 8 - sourcePoint.X / 8;
            for (int y = 0; y < size.Height; y++)
            {
                if (func == null)
                {
                    Unsafe.CopyBlock(destinationPointer, sourcePointer, (uint)stride);
                }
                else
                {
                    for (int x = 0; x < stride; x++)
                    {
                        *destinationPointer = func(*sourcePointer, *destinationPointer);

                        sourcePointer += 1;
                        destinationPointer += 1;
                    }

                    sourcePointer -= stride;
                    destinationPointer -= stride;
                }

                sourcePointer += Width;
                destinationPointer += bitmask.Width;
            }
        }

        public bool GetBit(int x, int y)
        {
            return ((*(Pointer + y * Width + x / 8)) & (128 >> (x % 8))) != 0;
        }

        public bool[] GetBits(Int32Rect rect)
        {
            int x0 = rect.X % 8;

            byte* pointer = Pointer + GetIndexAt(rect.X, rect.Y);

            bool[] bits = new bool[rect.Width * rect.Height];
            fixed (bool* bitsPtr = bits)
            {
                bool* bitsPointer = bitsPtr;
                for (int y = 0; y < rect.Height; y++)
                {
                    byte* nextPointer = pointer + Width;

                    byte b = *pointer;
                    b <<= x0;
                    int count = x0;

                    for (int x = 0; x < rect.Width; x++)
                    {
                        *bitsPointer = (b & 128) != 0;
                        bitsPointer += 1;
                        b <<= 1;
                        count++;
                        if (count == 8)
                        {
                            pointer += 1;
                            b = *pointer;
                            count = 0;
                        }
                    }

                    pointer = nextPointer;
                }
            }

            return bits;
        }

        public byte[] GetBits()
        {
            uint size = (uint)(Width * Height);
            byte[] bits = new byte[size];
            fixed (byte* bitsPointer = bits)
            {
                Unsafe.CopyBlock(bitsPointer, Pointer, size);
            }
            return bits;
        }

        public void SetBit(int x, int y)
        {
            *(Pointer + y * Width + x / 8) ^= (byte)(128 >> (x % 8));
        }
        
        public void SetBit(int x, int y, bool bit)
        {
            byte* pointer = Pointer + y * Width + x / 8;
            byte bits = (byte)(128 >> (x % 8));
            *pointer |= bits;
            if (!bit)
            {
                *pointer ^= bits;
            }
        }

        public void SetBits(int leftX, int rightX, int y)
        {
            int leftIndex = leftX / 8;
            int rightIndex = rightX / 8;

            byte* pointer = Pointer + y * Width + leftIndex;
            byte* lastPointer = pointer + (rightIndex - leftIndex);

            *pointer ^= (byte)(255 >> (leftX % 8));

            while (pointer != lastPointer)
            {
                pointer += 1;
                *pointer ^= 255;
            }

            *pointer ^= (byte)(255 >> (rightX % 8 + 1));
        }

        public void SetBits(int leftX, int rightX, int y, bool bit)
        {
            int leftIndex = leftX / 8;
            int rightIndex = rightX / 8;

            byte* pointer = Pointer + y * Width + leftIndex;
            byte* lastPointer = pointer + (rightIndex - leftIndex);

            byte lastBits = *lastPointer;

            byte bits = (byte)(255 >> (leftX % 8));
            *pointer |= bits;
            if (!bit)
            {
                *pointer ^= bits;
            }

            byte newBits = (byte)(bit ? 255 : 0);

            while (pointer != lastPointer)
            {
                pointer += 1;
                *pointer = newBits;
            }

            int shiftWidth1 = rightX % 8 + 1;
            int shiftWidth2 = 8 - shiftWidth1;
            lastBits <<= shiftWidth1;
            lastBits >>= shiftWidth1;
            *pointer >>= shiftWidth2;
            *pointer <<= shiftWidth2;
            *pointer |= lastBits;
        }

        public void SetBits(byte[] bits)
        {
            uint size = (uint)(Width * Height);
            if (bits.Length < size)
            {
                return;
            }

            fixed (byte* bitsPointer = bits)
            {
                Unsafe.CopyBlock(Pointer, bitsPointer, size);
            }
        }
    }
}
