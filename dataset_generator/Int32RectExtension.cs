using System.Windows;

namespace dataset_generator
{
    internal static class Int32RectExtension
    {
        public static Int32Point Point(this Int32Rect rect)
        {
            return new Int32Point(rect.X, rect.Y);
        }

        public static Int32Size Size(this Int32Rect rect)
        {
            return new Int32Size(rect.Width, rect.Height);
        }
    }
}
