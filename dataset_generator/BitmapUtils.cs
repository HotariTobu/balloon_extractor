using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace dataset_generator
{
    internal static class BitmapUtils
    {
        private record StackPoint(int X, int Y, bool IsDowning, int LeftX, int RightX);

        public static Int32Rect Fill(int pointX, int pointY, Func<int, int, bool> isBorder, Func<int, int, bool> isAlreadyDrawn, Action<int, int, int> draw)
        {
            if (isBorder(pointX, pointY))
            {
                return new Int32Rect(pointX, pointY, 0, 0);
            }

            int minX = pointX;
            int maxX = pointX;

            int minY = pointY;
            int maxY = pointY;

            Stack<StackPoint> stack = new Stack<StackPoint>();

            int leftX = pointX;
            int rightX = pointX;

            int currentY = pointY;

            extend();

            scan(leftX, rightX, false);
            scan(leftX, rightX, true);

            while (stack.Any())
            {
                StackPoint stackPoint = stack.Pop();

                if (isAlreadyDrawn(stackPoint.X, stackPoint.Y))
                {
                    continue;
                }

                leftX = stackPoint.X;
                rightX = stackPoint.X;

                currentY = stackPoint.Y;

                extend();

                scan(leftX, stackPoint.LeftX - 2, !stackPoint.IsDowning);
                scan(stackPoint.RightX + 2, rightX, !stackPoint.IsDowning);
                scan(leftX, rightX, stackPoint.IsDowning);
            }

            return new Int32Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);

            void extend()
            {
                while (!isBorder(--leftX, currentY)) ;
                while (!isBorder(++rightX, currentY)) ;

                leftX++;
                rightX--;

                draw(leftX, rightX, currentY);

                if (minX > leftX)
                {
                    minX = leftX;
                }

                if (maxX < rightX)
                {
                    maxX = rightX;
                }

                if (minY > currentY)
                {
                    minY = currentY;
                }
                else if (maxY < currentY)
                {
                    maxY = currentY;
                }
            }

            void scan(int leftX, int rightX, bool isDowning)
            {
                int y = currentY + (isDowning ? 1 : -1);

                bool isOnBorder = true;

                for (int x = leftX; x <= rightX; x++)
                {
                    if (isOnBorder)
                    {
                        if (!isBorder(x, y))
                        {
                            isOnBorder = false;

                            if (!isAlreadyDrawn(x, y))
                            {
                                StackPoint stackPoint = new StackPoint(x, y, isDowning, leftX, rightX);
                                stack.Push(stackPoint);
                            }
                        }
                    }
                    else if (isBorder(x, y))
                    {
                        isOnBorder = true;
                    }
                }
            }
        }

        public static Int32Rect DrawLine(Point point1, Point point2, Action<int, int, int> draw)
        {
            if (point1.Y > point2.Y)
            {
                Point temp = point1;
                point1 = point2;
                point2 = temp;
            }

            int x1 = (int)point1.X;
            int y1 = (int)point1.Y;
            int x2 = (int)point2.X;
            int y2 = (int)point2.Y;

            if (point1.Y == point2.Y)
            {
                subDraw(x1, x2, y1);
            }
            else
            {
                double m = (point1.X - point2.X) / (point1.Y - point2.Y);

                int lastX = getX(y2);
                subDraw(x2, lastX, y2);

                for (int y = y2 - 1; y > y1; y--)
                {
                    int x = getX(y);
                    subDraw(lastX, x, y);
                    lastX = x;
                }

                subDraw(lastX, x1, y1);

                int getX(int y)
                {
                    return (int)(m * (y - point1.Y) + point1.X);
                }
            }

            return new Int32Rect(Math.Min(x1, x2), y1, Math.Abs(x2 - x1) + 1, y2 - y1 + 1);

            void subDraw(int x1, int x2, int y)
            {
                if (x1 < x2)
                {
                    draw(x1, x2, y);
                }
                else
                {
                    draw(x2, x1, y);
                }
            }
        }
    }
}
