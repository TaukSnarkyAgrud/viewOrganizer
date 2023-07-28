using System.Drawing;

namespace viewTools
{
    public class ViewSize
    {
        public Size size;

        public static implicit operator int(ViewSize s) => s.size.Width == 0 && s.size.Height == 0 ? 0 : -1;

        public override string ToString()
        {
            return $"({size.Width},{size.Height})";
        }
        public ViewSize(int w, int h)
        {
            size = new Size(w, h);
            size.Width = w;
            size.Height = h;
        }
    }
}
