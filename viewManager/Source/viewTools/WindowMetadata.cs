using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using static viewTools.DataStructs;

namespace viewTools
{
    public class WindowMetadata
    {
        public static Dictionary<IntPtr ,WindowMetadata> RootWindows;

        public IntPtr handle;
        public string title;
        public Process mainProcess;
        public Dictionary<IntPtr, WindowMetadata> children;

        public WINDOW_POSITION position;
        public WINDOW_SIZE size;
        public string ViewState;

        public bool hasIntermediateParent;
        public IntPtr immediateParentHandle;

        public bool isViableWindow = false;

        public WindowMetadata()
        {

        }

        public WindowMetadata(IntPtr handle)
        {
            this.handle = handle;
            WindowMetadata rootParent;
            
            if (IsRootParent(handle))
            {
                //Add the root
                GetAddRootObject(handle, out rootParent);
            } else
            {
                // Its a child of some parent, add its root
                GetAddRootObject(GetRootParent(handle), out rootParent);

                // Check if immediate parent is root or intermediate
                hasIntermediateParent = HasIntermediateParent(handle, rootParent.handle);
            }

            // Add immediate children
            AddImmediateChildren();
        }

        public override string ToString()
        {
            return $"Window Object {handle} 0x{handle.ToString("x8")} | {title} | {mainProcess} | {children} | {position} | {size} | {ViewState} | {hasIntermediateParent} | {immediateParentHandle} | {isViableWindow}";
        }

        private void AddImmediateChildren()
        {
            foreach (var child in GetAllChildrenFromSystem())
            {
                GetAddChildObject(child, out WindowMetadata _);
            }
        }

        private List<IntPtr> GetAllChildrenFromSystem()
        {
            var childPtr = handle;
            var childrenPtrs = new List<IntPtr>();
            while((int)childPtr != 0)
            {
                if (childPtr != handle)
                {
                    childrenPtrs.Add(childPtr);
                    childPtr = WindowsAPITools.FindWindowExAWrapper(handle, childPtr, null, null);
                }
                else
                {
                    childPtr = WindowsAPITools.FindWindowExAWrapper(handle, IntPtr.Zero, null, null);
                }

                
            }
            return childrenPtrs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="possiblyCreatedObject"></param>
        /// <returns>True if the object was returned without being created.</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        private bool GetAddRootObject(IntPtr handle, out WindowMetadata possiblyCreatedObject)
        {
            if (RootWindows == null)
            {
                RootWindows = new
                    Dictionary<IntPtr, WindowMetadata>();
            }
            if (RootWindows.TryGetValue(handle, out WindowMetadata value))
            {
                possiblyCreatedObject = value;
                return true;
            }
            possiblyCreatedObject = AddRootObject(handle);
            return true;
        }

        private WindowMetadata AddRootObject(IntPtr handle)
        {
            position = WindowsAPITools.GetWindowPosition(handle);
            size = WindowsAPITools.GetWindowSize(handle);
            ViewState = WindowsAPITools.GetWindowViewState(handle);
            filterOutNonUserWindowObjects(this);
            HasTitle();
            RootWindows.Add(handle, this);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="possiblyCreatedObject"></param>
        /// <returns>True if the object was returned without being created.</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        private bool GetAddChildObject(IntPtr handle, out WindowMetadata possiblyCreatedObject)
        {
            if (children == null)
            {
                children = new
                    Dictionary<IntPtr, WindowMetadata>();
            }
            if (children.TryGetValue(handle, out WindowMetadata value))
            {
                possiblyCreatedObject = value;
                return true;
            }
            possiblyCreatedObject = AddChildObject(handle);
            return true;
        }

        private WindowMetadata AddChildObject(IntPtr handle)
        {
            var windowObjectCreation = new WindowMetadata();
            windowObjectCreation.handle = handle;
            windowObjectCreation.position = WindowsAPITools.GetWindowPosition(handle);
            windowObjectCreation.size = WindowsAPITools.GetWindowSize(handle);
            windowObjectCreation.ViewState = WindowsAPITools.GetWindowViewState(handle);
            filterOutNonUserWindowObjects(windowObjectCreation);
            windowObjectCreation.HasTitle();
            
            children.Add(handle, windowObjectCreation);
            return windowObjectCreation;
        }

        private void GetChildren()
        {
            throw new NotImplementedException();
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

        public bool IsRootParent(IntPtr hwnd)
        {
            if (GetRootParent(hwnd) == hwnd)
            {
                return true;
            }
            return false;
        }
        public IntPtr GetParent(IntPtr hwnd)
        {
            return WindowsAPITools.GetParentWrapper(hwnd);
        }
        public bool HasIntermediateParent(IntPtr hwnd, IntPtr rootHandle)
        {
            if (rootHandle == (immediateParentHandle = GetParent(hwnd)))
            {
                return false;
            }
            return true;
        }

        public static void filterOutNonUserWindowObjects(WindowMetadata aProspectiveWindowObject)
        {
            if (aProspectiveWindowObject.position == 0 
                || aProspectiveWindowObject.size == 0 
                || aProspectiveWindowObject.ViewState == ShowWindowCommands.Hide.ToString())
            {
                aProspectiveWindowObject.isViableWindow = false;
                return;
            }

            if (aProspectiveWindowObject.HasTitle())
            {
                aProspectiveWindowObject.isViableWindow = true;
            }
        }

        private bool HasTitle()
        {
            if (title == null || title.Count() > 0)
            {
                return true;
            }
            WindowsAPITools.GetWindowTextA(handle, out title, 100);
            if (title.Count() > 0)
            {
                return true;
            }
            return false;
        }
    }
}
