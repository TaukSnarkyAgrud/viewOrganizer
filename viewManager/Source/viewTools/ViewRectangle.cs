using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static viewTools.DataStructs;

namespace viewTools
{
    public class ViewRectangle
    {
        public Rectangle rectangle;
        public ViewPosition position
        {
            get
            {
                return new ViewPosition(rectangle.X, rectangle.Y);
            }

            set
            {
                rectangle.X = value.left;
                rectangle.Y = value.top;
            }
        }
        public ViewSize size
        {
            get
            {
                return new ViewSize(rectangle.Width, rectangle.Height);
            }
            set
            {
                rectangle.Width = value.size.Width;
                rectangle.Height = value.size.Height;
            }
        }

        public int right => rectangle.Right;
        public int bottom => rectangle.Bottom;

        public ViewRectangle()
        {
            rectangle = new Rectangle(0, 0, 0, 0);
        }
        public ViewRectangle(int left, int top, Size s)
        {
            rectangle = new Rectangle(left, top, s.Width, s.Height);
        }
        public ViewRectangle(int left, int top, int right, int bottom)
        {
            rectangle = Rectangle.FromLTRB(left, top, right, bottom);
        }

        public List<ViewRectangle> TranslateAllToPositiveCoordinatePlane(List<ViewRectangle> inRectangles)
        {
            var outRectangles = new List<ViewRectangle>();

            // Check if any are negative
            if (AnyRectanglesInNegative(inRectangles, out var negativeEdges))
            {
                foreach (var item in inRectangles)
                {
                    var newX = Math.Abs(negativeEdges.X);
                    var newY = Math.Abs(negativeEdges.Y);
                    outRectangles.Add(new ViewRectangle(
                        item.position.left + newX,
                        item.position.top + newY,
                        new Size(item.right + newX, item.bottom + newY)));
                }
            }
            else
            {
                return inRectangles;
            }
            return outRectangles;
        }

        private bool AnyRectanglesInNegative(List<ViewRectangle> inRectangles, out Point negativeEdges)
        {
            var anyNegative = false;
            negativeEdges = new Point(0, 0);
            foreach (var rect in inRectangles)
            {
                if (rect.position.left < 0
                    || rect.position.top < 0)
                {
                    anyNegative = true;
                }

                if (rect.position.left < negativeEdges.X)
                {
                    negativeEdges.X = rect.position.left;
                }

                if (rect.position.top < negativeEdges.Y)
                {
                    negativeEdges.Y = rect.position.top;
                }
            }

            return anyNegative;
        }
    }
}
