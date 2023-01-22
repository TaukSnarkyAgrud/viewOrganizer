using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using static viewTools.DataStructs;

namespace viewTools
{
    public class WindowMetadata
    {
        public IntPtr handle;
        public string title;
        public Process mainProcess;
        public IntPtr rootParentHandle;
        public Dictionary<IntPtr, WindowMetadata> children;

        public ViewRectangle rectangle;
        public string ViewState;

        public bool hasIntermediateParent;
        public IntPtr immediateParentHandle;

        public bool isViableWindow = false;

        public ViewPosition position
        {
            get
            {
                return rectangle.position;
            }
            set
            {
                rectangle.position = value;
            }
        }
        public ViewSize size
        {
            get
            {
                return rectangle.size;
            }
            set
            {
                rectangle.size = value;
            }
        }


        public WindowMetadata()
        {
            rectangle = new ViewRectangle();
        }

        public WindowMetadata(IntPtr handle): this()
        {
            this.handle = handle;
        }

        public WindowMetadata(IntPtr handle, Process mainProcess):this(handle)
        {
            this.mainProcess = mainProcess;
        }


        public override string ToString()
        {
            return $"Window Object: {GetStringOrPlaceholder(mainProcess?.ProcessName, "mainProcessName")}" +
                $" | {GetStringOrPlaceholder(handle.ToString(), nameof(handle))} 0x{handle.ToString("x8")}" +
                $" | {GetStringOrPlaceholder(title, nameof(title))}" +
                $" | {GetStringOrPlaceholder(mainProcess?.Id.ToString(), "mainProcessId")}" +
                $" | {GetStringOrPlaceholder(children?.ToString(), nameof(children))}" +
                $"| {GetStringOrPlaceholder(position.ToString(), nameof(position))}" +
                $" | {GetStringOrPlaceholder(size.ToString(), nameof(size))}" +
                $" | {GetStringOrPlaceholder(ViewState, nameof(ViewState))}" +
                $" | {GetStringOrPlaceholder(hasIntermediateParent.ToString(), nameof(hasIntermediateParent))}" +
                $" | {GetStringOrPlaceholder(immediateParentHandle.ToString(), nameof(immediateParentHandle))}" +
                $" | {GetStringOrPlaceholder(isViableWindow.ToString(), nameof(isViableWindow))}";
        }

        private object GetStringOrPlaceholder(string str, string placeholder)
        {
            if (placeholder == "children" && string.IsNullOrEmpty(str))
            {
                return "<Is LeafChild>";
            }
            if (string.IsNullOrEmpty(str))
            {
                return $"<{placeholder}>";
            }
            if (placeholder == "children")
            {
                var retString = "";
                foreach (var child in children.Keys)
                {
                    retString+= child.ToString() + " " ;
                }
                return retString;
            }
            return str;
        }


        public IntPtr GetRootParent(IntPtr hwnd)
        {
            IntPtr hwndParent = WindowsAPITools.GetParentWrapper(hwnd);
            if (hwndParent == IntPtr.Zero)
            {
                return hwnd;
            }
            else
            {
                return GetRootParent(hwndParent);
            }
        }

        public bool IsRootParent()
        {
            var rootPtr = GetRootParent(this.handle);
            if (rootPtr == this.handle)
            {
                return true;
            } else
            {
                this.rootParentHandle = rootPtr;
            }
            return false;
        }

        public IntPtr GetImmediateParent(IntPtr hwnd)
        {
            return WindowsAPITools.GetParentWrapper(hwnd);
        }

        public bool HasIntermediateParent(IntPtr hwnd, IntPtr rootHandle)
        {
            if (rootHandle == (immediateParentHandle = GetImmediateParent(hwnd)))
            {
                return false;
            }
            return true;
        }

        public bool HasTitle()
        {
            if (title != null && title.Count() > 0)
            {
                return true;
            }

            // Call external method asking api to get windows title; set title field
            WindowsAPITools.GetWindowTextAWrapper(handle, out title, 100);

            // Check if the title was set
            if (title.Count() > 0)
            {
                return true;
            }
            return false;
        }
    }
}
