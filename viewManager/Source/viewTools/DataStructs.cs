using System.Drawing;

namespace viewTools
{
    public class DataStructs
    {
        public struct WINDOW_POSITION

        {

            public int Left;        // x position of upper-left corner

            public int Top;         // y position of upper-left corner

            public static implicit operator int(WINDOW_POSITION p) => p.Left == 0 && p.Top == 0 ? 0 : -1;

            public override string ToString()
            {
                return $"({Left},{Top})";
            }

        }

        public struct WINDOW_SIZE

        {

            public int Width;        // x position of lower-right corner

            public int Height;         // y position of lower-right corner

            public static implicit operator int(WINDOW_SIZE s) => s.Width == 0 && s.Height == 0 ? 0 : -1;

            public override string ToString()
            {
                return $"({Width},{Height})";
            }

        }

        public enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            MAXIMIZE = 3,
            SHOWNOACTIVATE = 4,
            SHOW = 5,
            MINIMIZE = 6,
            SHOWMINNOACTIVE = 7,
            SHOWNA = 8,
            RESTORE = 9,
            SHOWDEFAULT = 10,
            FORCEMINIMIZE = 11,
        }
    }
}
