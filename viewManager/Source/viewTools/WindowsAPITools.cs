using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace viewTools
{
    class WindowsAPITools
    {

        public  delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetFocus(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT wPlmt);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I8)]
        public static extern long SetWindowLongA(IntPtr window, int nIndex, long dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BringWindowToTop(IntPtr window);

        public struct RECT

        {

            public int Left;        // x position of upper-left corner

            public int Top;         // y position of upper-left corner

            public int Right;       // x position of lower-right corner

            public int Bottom;      // y position of lower-right corner

        }

        public struct WINDOW_POSITION

        {

            public int Left;        // x position of upper-left corner

            public int Top;         // y position of upper-left corner

        }

        public struct WINDOW_SIZE

        {

            public int Width;        // x position of lower-right corner

            public int Height;         // y position of lower-right corner

        }

        public struct POINT

        {

            public long x;

            public long y;

        }

        public struct WINDOWPLACEMENT

        {

            public uint length;

            public uint flags;

            public uint showCmd;

            public POINT ptMinPosition;

            public POINT ptMaxPosition;

            public RECT rcNormalPosition;

            public RECT rcDevice;

        }

        private enum ShowWindowCommands : int
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

        enum WindowLongFlags : int
        {
            GWL_EXSTYLE = -20,
            GWLP_HINSTANCE = -6,
            GWLP_HWNDPARENT = -8,
            GWL_ID = -12,
            GWL_STYLE = -16,
            GWL_USERDATA = -21,
            GWL_WNDPROC = -4,
            DWLP_USER = 0x8,
            DWLP_MSGRESULT = 0x0,
            DWLP_DLGPROC = 0x4
        }

        public static void ConfigureWindowSizePosition(IntPtr hwnd, int x = 0, int y = 0, int nWidth = 200, int nHeight = 100)
        {
            var success = MoveWindow(hwnd, x, y, nWidth, nHeight, true);
            if (!success)
            {
                Console.WriteLine(Marshal.GetLastWin32Error());
            }
        }

        public static void RemoveTitlebar(IntPtr hwnd)
        {
            SetWindowLongA(hwnd, (int)WindowLongFlags.GWL_STYLE, 0x00C00000L);
        }

        public static void HideWindow(IntPtr hwnd)
        {
            ShowWindow(hwnd, (int)ShowWindowCommands.Hide);
        }

        public static void RestoreWindow(IntPtr hwnd)
        {
            ShowWindow(hwnd, (int)ShowWindowCommands.RESTORE);
        }

        public static void MaximizeWindow(IntPtr hwnd)
        {
            ShowWindow(hwnd, (int)ShowWindowCommands.MAXIMIZE);
        }

        public static void MinimizeWindow(IntPtr hwnd)
        {
            ShowWindow(hwnd, (int)ShowWindowCommands.Minimized);
        }

        public static void ForceMinimizeWindow(IntPtr hwnd)
        {
            ShowWindow(hwnd, (int)ShowWindowCommands.FORCEMINIMIZE);
        }

        public static WINDOW_POSITION GetWindowPosition(IntPtr hwnd)
        {
            bool success = GetWindowRect(hwnd, out RECT position);
            if (!success)
            {
                Console.WriteLine(Marshal.GetLastWin32Error());
            }

            var posi = new WINDOW_POSITION();
            posi.Top = position.Top;
            posi.Left = position.Left;

            return posi;
        }

        public static WINDOW_SIZE GetWindowSize(IntPtr hwnd)
        {
            var position = new RECT();
            bool success = GetWindowRect(hwnd, out position);
            if (!success)
            {
                Console.WriteLine(Marshal.GetLastWin32Error());
            }

            var size = new WINDOW_SIZE();
            size.Width = position.Bottom - position.Top;
            size.Height = position.Right - position.Left;

            return size;
        }

        public static string GetWindowViewState(IntPtr hwnd)
        {
            bool success = GetWindowPlacement(hwnd, out WINDOWPLACEMENT placement);
            if (!success)
            {
                Console.WriteLine(Marshal.GetLastWin32Error());
            }

            return Enum.GetName(typeof(ShowWindowCommands), placement.showCmd);
        }

        // https://stackoverflow.com/questions/1363167/how-can-i-get-the-child-windows-of-a-window-given-its-hwnd
        public List<IntPtr> GetAllChildHandles(IntPtr hwnd)
        {
            List<IntPtr> childHandles = new List<IntPtr>();

            GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
            IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(hwnd, childProc, pointerChildHandlesList);
            }
            finally
            {
                gcChildhandlesList.Free();
            }

            return childHandles;
        }

        private bool EnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);

            if (gcChildhandlesList == null || gcChildhandlesList.Target == null)
            {
                return false;
            }

            List<IntPtr> childHandles = gcChildhandlesList.Target as List<IntPtr>;
            childHandles.Add(hWnd);

            return true;
        }
    }
}
