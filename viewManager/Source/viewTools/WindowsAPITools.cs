using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static viewTools.DataStructs;

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
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int pId);

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

        [DllImport("user32.dll", ExactSpelling = true)]
        static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

        [DllImport("user32.dll")]
        static extern IntPtr GetLastActivePopup(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetTitleBarInfo(IntPtr hwnd, out TITLEBARINFO pti);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

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

        [StructLayout(LayoutKind.Sequential)]
        struct TITLEBARINFO
        {
            public const int CCHILDREN_TITLEBAR = 5;
            public uint cbSize;
            public RECT rcTitleBar;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = CCHILDREN_TITLEBAR + 1)]
            public uint[] rgstate;
        }

        enum GetAncestorFlags
        {
            /// <summary>
            /// Retrieves the parent window. This does not include the owner, as it does with the GetParent function.
            /// </summary>
            GetParent = 1,
            /// <summary>
            /// Retrieves the root window by walking the chain of parent windows.
            /// </summary>
            GetRoot = 2,
            /// <summary>
            /// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
            /// </summary>
            GetRootOwner = 3
        }
        
        enum TBStates
        {
            STATE_SYSTEM_UNAVAILABLE = 0x1,
            STATE_SYSTEM_PRESSED = 0x8,
            STATE_SYSTEM_INVISIBLE = 0x8000,
            STATE_SYSTEM_OFFSCREEN = 0x10000,
            STATE_SYSTEM_FOCUSABLE = 0x100000
        }

        public static bool IsAltTabWindow(IntPtr hwnd)
        {
            TITLEBARINFO ti;
            IntPtr hwndTry, hwndWalk = new IntPtr(1);

            if (!IsWindowVisible(hwnd))
                return false;

            hwndTry = GetAncestor(hwnd, GetAncestorFlags.GetRootOwner);
            while (hwndTry != hwndWalk)
            {
                hwndWalk = hwndTry;
                hwndTry = GetLastActivePopup(hwndWalk);
                if (IsWindowVisible(hwndTry))
                    break;
            }
            if (hwndWalk != hwnd)
                return false;

            // the following removes some task tray programs and "Program Manager"
            ti.cbSize = (uint)Marshal.SizeOf(typeof(TITLEBARINFO));
            GetTitleBarInfo(hwnd, out ti);
            if ((ti.rgstate[0] & 0x8000) == 0x8000)
                return false;

            // Tool windows should not be displayed either, these do not appear in the
            // task bar.
            if (((int)GetWindowLongPtr(hwnd, -20) & 0x00000080) == 0x00000080)
                return false;

            return true;
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
            // TODO: Does this return itself if it has no parent?
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

            // TODO: Test this and note if its broken or unnecessary.
            if (gcChildhandlesList == null || gcChildhandlesList.Target == null)
            {
                return false;
            }

            List<IntPtr> childHandles = gcChildhandlesList.Target as List<IntPtr>;
            childHandles.Add(hWnd);

            return true;
        }

        //public static void RemoveTitlebar(IntPtr hwnd)
        //{
        //    SetWindowLongA(hwnd, (int)WindowLongFlags.GWL_STYLE, 0x00C00000L);
        //}
    }
}
