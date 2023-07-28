using System.Drawing;

namespace viewTools
{
    public class ViewPosition
    {
        public Point p;

        public int left
        {
            get
            {
                return p.X;
            }

            set
            {
                p.X = value;
            }
        }

        public int top
        {
            get
            {
                return p.Y;
            }

            set
            {
                p.Y = value;
            }
        }


        public static implicit operator int(ViewPosition p) => p.p.X == 0 && p.p.Y == 0 ? 0 : -1;

        public ViewPosition(int l, int t)
        {
            p = new Point(l, t);
        }

        public override string ToString()
        {
            return $"({left},{top})";
        }
    }
}
