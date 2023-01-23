using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static viewTools.DataStructs;
using static viewTools.WindowsAPITools;

namespace viewTools
{
    class WindowsAPITools
    {
        public delegate bool EnumWindowsProc(IntPtr hwnd, int lParam);

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
        public static extern bool EnumChildWindows(IntPtr window, EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern long SetWindowLongA(IntPtr window, int nIndex, long dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BringWindowToTop(IntPtr window);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowExA(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowTextW(IntPtr hwnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextLength", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool EnumWindows(EnumWindowsProc callback, int extraData);

        public struct RECT

        {

            public int Left;        // x position of upper-left corner

            public int Top;         // y position of upper-left corner

            public int Right;       // x position of lower-right corner

            public int Bottom;      // y position of lower-right corner

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

        public static bool GetWindowTextWrapper(IntPtr hwnd, out string lpString, int nMaxCount)
        {
            var length = GetWindowTextLength(hwnd);
            if (length < 1)
            {
                lpString = "";
                return false;
            }
            StringBuilder lpStringReturn = new(length + 1);
            var successValue = GetWindowTextW(hwnd, lpStringReturn, lpStringReturn.Capacity);

            Debug.WriteLine($"GetWindowTextA for Title returned: {successValue}");
            if (successValue > 0)
            {
                lpString = lpStringReturn == null ? "" : lpStringReturn.ToString();
                return true;
            }
            lpString = lpStringReturn == null ? "" : lpStringReturn.ToString();
            return false;
        }

        public static IntPtr GetParentWrapper(IntPtr hwnd)
        {
            return GetParent(hwnd);
        }

        public static IntPtr FindWindowExAWrapper(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string lpszWindow)
        {
            // TODO: Add optimizaion of caching values from calles; make the values in the list have a time to live
            return FindWindowExA(hWndParent, hWndChildAfter, lpszClass, lpszWindow);
        }

        public static ViewPosition GetWindowPosition(IntPtr hwnd)
        {
            bool success = GetWindowRect(hwnd, out RECT position);
            if (!success)
            {
                Debug.WriteLine(Marshal.GetLastWin32Error());
            }

            var posi = new ViewPosition(position.Left, position.Top);

            return posi;
        }

        public static ViewSize GetWindowSize(IntPtr hwnd)
        {
            var position = new RECT();
            bool success = GetWindowRect(hwnd, out position);
            if (!success)
            {
                Debug.WriteLine(Marshal.GetLastWin32Error());
            }

            var size = new ViewSize(position.Right - position.Left, position.Bottom - position.Top);

            return size;
        }

        public static void ConfigureWindowSizePosition(IntPtr hwnd, int x = 0, int y = 0, int nWidth = 200, int nHeight = 100)
        {
            var success = MoveWindow(hwnd, x, y, nWidth, nHeight, true);
            if (!success)
            {
                Debug.WriteLine(Marshal.GetLastWin32Error());
            }
        }

        public static string GetWindowViewState(IntPtr hwnd)
        {
            bool success = GetWindowPlacement(hwnd, out WINDOWPLACEMENT placement);
            if (!success)
            {
                Debug.WriteLine(Marshal.GetLastWin32Error());
            }

            return Enum.GetName(typeof(ShowWindowCommands), placement.showCmd);
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

        public static IntPtr GetAnyWindowByClass(string className)
        {
            return FindWindowExA(IntPtr.Zero, IntPtr.Zero, lpszClass: className, null);
        }

        public static IntPtr GetAnyChromeWigetHandle()
        {
            return GetAnyWindowByClass("Chrome_WidgetWin_1");
        }

        public static List<IntPtr> GetAllChromeWigetHandles()
        {
            var wgts = new List<IntPtr>
            {
                GetAnyWindowByClass("Chrome_WidgetWin_1")
            };


            var frsthdl = wgts.First();
            var sibling = frsthdl;
            while ((int)sibling != 0)
            {
                if (sibling != frsthdl)
                {
                    wgts.Add(sibling);
                }

                sibling = WindowsAPITools.FindWindowExAWrapper(IntPtr.Zero, sibling, "Chrome_WidgetWin_1", null);
            }

            return wgts;
        }






        // Unverified Methods

        // https://stackoverflow.com/questions/1363167/how-can-i-get-the-child-windows-of-a-window-given-its-hwnd
        //public static List<IntPtr> GetAllChildHandles(IntPtr hwnd)
        //{
        //    List<IntPtr> childHandles = new List<IntPtr>();

        //    GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
        //    IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

        //    try
        //    {
        //        EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
        //        EnumChildWindows(hwnd, childProc, pointerChildHandlesList);
        //    }
        //    finally
        //    {
        //        gcChildhandlesList.Free();
        //    }

        //    return childHandles;
        //}

        private static bool EnumWindow(IntPtr hWnd, IntPtr lParam)
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

        public static void RemoveTitlebar(IntPtr hwnd)
        {
            SetWindowLongA(hwnd, (int)WindowLongFlags.GWL_STYLE, 0x00C00000L);
        }
    }
}
