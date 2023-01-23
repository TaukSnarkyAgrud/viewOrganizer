using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Linq;
using System.Drawing;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using static viewTools.DataStructs;

namespace viewTools
{
    public class WindowView
    {
        public WindowMetadata wInfo;
        public DisplayView dView;
        public List<ViewRectangle> windowRectanglesPositive;

        public Dictionary<IntPtr, WindowMetadata> rootWindows;
        private List<IntPtr> windowPointersEnumerated;
        private static List<IntPtr> windowPointersFromApiEnumerated;

        public List<IntPtr> WindowPointersEnumerated {
            get
            {
                if (windowPointersEnumerated == null)
                {
                    List<IntPtr> items = new List<IntPtr>();
                    foreach (var item in rootWindows.Select(v => v.Value).ToList())
                    {
                        items.Add(item.handle);
                        items.AddRange(GetAllChildren(item));
                    }
                    return items;
                }
                return windowPointersEnumerated;
            }
            
            set
            {
                List<IntPtr> items = new List<IntPtr>();
                foreach (var item in rootWindows.Select(v => v.Value).ToList())
                {
                    items.Add(item.handle);
                    items.AddRange(GetAllChildren(item));
                }
                windowPointersEnumerated= items;
            }
        }
        public List<WindowMetadata> AllWindowObjectsEnumerated
        {
            get
            {
                if (rootWindows == null)
                {
                    return new List<WindowMetadata>();
                }
                return EnumnerateWindows(rootWindows);
            }
        }

        public WindowView(DisplayView view)
        {
            dView = view;
            IngestAllWindowObjects();

            // Prints manifest
            rootWindows ??= new
                    Dictionary<IntPtr, WindowMetadata>();
            foreach (var item in rootWindows.Select(v => v.Value).ToList())
            {
                Debug.WriteLine(item);
                if (item.children != null)
                {
                    foreach (var child in item.children.Values.ToList<WindowMetadata>())
                    {
                        Debug.WriteLine("C    " + child);
                    }
                    Debug.WriteLine("");
                }
            }
        }

        public List<WindowMetadata> EnumnerateWindows(Dictionary<IntPtr, WindowMetadata> someWindows)
        {
            return GetEnumeratedWindows(someWindows).Values.ToList<WindowMetadata>();
        }

        private Dictionary<IntPtr, WindowMetadata> GetEnumeratedWindows(Dictionary<IntPtr, WindowMetadata> someWindows)
        {
            var windowsAsDict = new Dictionary<IntPtr, WindowMetadata>();
            foreach (var item in someWindows.Keys)
            {
                if (!windowsAsDict.ContainsKey(item))
                {
                    windowsAsDict.Add(item, someWindows[item]);
                }

                var children = someWindows[item].children;
                if (children != null && children.Count > 0)
                {
                    foreach (var child in GetEnumeratedWindows(children).Values)
                    {
                        if (!windowsAsDict.ContainsKey(child.handle))
                        {
                            windowsAsDict.Add(child.handle, child);
                        }
                    }
                }
                
            }
            return windowsAsDict;
        }

        private List<IntPtr> GetAllChildren(WindowMetadata aWindow)
        {
            List<IntPtr> items = new List<IntPtr>();
            var aWindowChildren = aWindow.children?.Select(v => v.Key).ToList();
            if(aWindowChildren != null)
            {
                // Add any children, if any
                items.AddRange(aWindowChildren);
                foreach (var child in aWindowChildren)
                {
                    // Add childs' children, if any
                    items.AddRange(GetAllChildren(aWindow));
                }
            }
            return items;
        }

        public List<Process> GetAllProcessesWithWindows()
        {
            return Process.GetProcesses().Where(p => (int)p.MainWindowHandle != 0).ToList();
        }

        public void PrintAllProcessesWithWindows()
        {
            var procs = GetAllProcessesWithWindows();
            var procnum = 1;
            try
            {
                foreach (var proc in procs)
                {
                    Debug.WriteLine($"------------------------------------------------------------------ Process {procnum} ({proc.Id})------------------------------------------------------------------\n");
                    foreach (var prop in proc.GetType().GetProperties())
                    {
                        try
                        {
                            if (prop.ToString().Contains("MainWindowTitle"))
                            {
                                Debug.Write($"{prop} = ");
                                Debug.WriteLine($"{prop.GetValue(proc)}");
                            }
                        }
                        catch (Exception)
                        {

                            Debug.WriteLine("exception thrown");
                        }
                    }
                    procnum++;
                }
            }
            catch (Exception)
            {
            }
        }

        public Process GetWindowProcessMatchTitleWord(string subString)
        {
            var possibleProcs = GetWindowProcessesMatchTitleWord(subString);
            if (possibleProcs.Count == 1)
            {
                return possibleProcs.FirstOrDefault();
            }
            else if (possibleProcs.Count == 1)
            {
                throw new Exception($"No process found contining criteria: \"{subString}\"");
            }
            else if (possibleProcs.Count > 1)
            {
                throw new Exception($"More than one process found contining criteria: \"{subString}\"");
            }
            return new Process();
        }

        public Process GetWindowProcessMatchProcessName(string subString)
        {
            var possibleProcs = GetWindowProcessesMatchProcessName(subString);
            if (possibleProcs.Count == 1)
            {
                return possibleProcs.FirstOrDefault();
            }
            else if (possibleProcs.Count == 1)
            {
                throw new KeyNotFoundException($"No process found contining criteria: \"{subString}\"");
            }
            else if (possibleProcs.Count > 1)
            {
                throw new KeyNotFoundException($"More than one process found contining criteria: \"{subString}\"");
            }
            return new Process();
        }

        public IntPtr GetWindowHandleMatchTitleWord(string subString)
        {
            return GetWindowProcessMatchTitleWord(subString).MainWindowHandle;
        }

        public IntPtr GetWindowHandleMatchProcessName(string subString)
        {
            return GetWindowProcessMatchProcessName(subString).MainWindowHandle;
        }

        public List<Process> GetWindowProcessesMatchTitleWord(string subString)
        {
            var viableProcs = new List<Process>();
            foreach (var proc in GetAllProcessesWithWindows())
            {
                if (proc.MainWindowTitle.ToString().Contains(subString))
                {
                    viableProcs.Add(proc);
                }
            }
            return viableProcs;
        }

        public List<Process> GetWindowProcessesMatchProcessName(string subString)
        {
            var viableProcs = new List<Process>();
            foreach (var proc in GetAllProcessesWithWindows())
            {
                if (proc.ProcessName.ToString().Contains(subString))
                {
                    viableProcs.Add(proc);
                }
            }
            return viableProcs;
        }

        public void ConfigureWindowSizePosition(IntPtr hwnd, int x, int y, int width, int height)
        {
            WindowsAPITools.ConfigureWindowSizePosition(hwnd, x, y, width, height);
        }

        public void BringWindowTop(IntPtr hwnd)
        {
            WindowsAPITools.MinimizeWindow(hwnd);
            WindowsAPITools.RestoreWindow(hwnd);
            WindowsAPITools.BringWindowToTop(hwnd);
        }

        public void RemoveTitleBar(IntPtr hwnd)
        {
            WindowsAPITools.RemoveTitlebar(hwnd);
        }

        //public List<IntPtr> GetAllChildHandles(IntPtr hwnd)
        //{
        //    return WindowsAPITools.GetAllChildHandles(hwnd);
        //}

        //public List<IntPtr> GetAllChildChromeWigets(IntPtr parent)
        //{
        //    var anyWgtHdl = WindowsAPITools.GetAnyChromeWigetHandle();
        //    Debug.WriteLine(anyWgtHdl);
        //    var nxtHdl = WindowsAPITools.FindWindowExA(IntPtr.Zero, anyWgtHdl, "Chrome_WidgetWin_1", null);
        //    Debug.WriteLine(nxtHdl);
        //    return GetAllChildHandles(anyWgtHdl);
        //}

        public void IngestAllWindowObjects()
        {
            IngestChromeWindows();
            IngestWindowsByProcess();
            IngestWindowsByAPI();
        }

        private void IngestWindowsByAPI()
        {
            windowPointersFromApiEnumerated = new List<IntPtr>();
            WindowsAPITools.EnumWindows(EnumWindowsCallback, 0);
            foreach (var item in windowPointersFromApiEnumerated)
            {
                var alreadyAware = AllWindowObjectsEnumerated.FirstOrDefault(x => x.handle == item);
                if (alreadyAware != null)
                {
                    continue;
                }
                var newWM = new WindowMetadata(item);
                GetAddWindow(newWM);
            }
        }

        public static bool EnumWindowsCallback(IntPtr hwnd, int lParam)
        {
            WindowView.windowPointersFromApiEnumerated.Add(hwnd);
            return true;
        }

        private void IngestWindowsByProcess()
        {

            Debug.WriteLine($"\nOther Processes with handles found\n---------------------------------");
            var windowProcesses = GetAllProcessesWithWindows();
            foreach (var item in windowProcesses)
            {
                var alreadyAware = AllWindowObjectsEnumerated.FirstOrDefault(x => x.handle == item.MainWindowHandle);
                if(alreadyAware != null)
                {
                    alreadyAware.mainProcess = item;
                    continue;
                }
                var newWM = new WindowMetadata(item.MainWindowHandle, item);
                GetAddWindow(newWM);
            }
        }

        public void IngestChromeWindows()
        {
            try
            {
                var wgts = new List<IntPtr>();
                var prcs = GetWindowProcessesMatchProcessName("chrome");
                if (prcs.Count > 0)
                {
                    Debug.WriteLine("\n\nChrome process found");
                    WindowsAPITools.GetAllChromeWigetHandles();
                    foreach (var item in wgts)
                    {
                        var newWM = new WindowMetadata(item);
                        GetAddWindow(newWM);
                    }
                }
            }
            catch (KeyNotFoundException ex)
            {
                Debug.WriteLine("No chrome windows found");
                Debug.WriteLine(ex.Message);
            }
        }

        private void GetAddWindow(WindowMetadata newWM)
        {
            rootWindows ??= new
                    Dictionary<IntPtr, WindowMetadata>();
            // If it already exists, ignore
            if (WindowViewIsAwareOfThisWindow(newWM))
            {
                return;
            }

            if (newWM.IsRootParent())
            {
                //Add the root
                GetAddRootObject(newWM.handle, out _);
            }
            else
            {
                // Its a child of some parent, add its root
                GetAddWindow(new WindowMetadata(newWM.rootParentHandle));

                // Check if immediate parent is root or intermediate
                newWM.hasIntermediateParent = newWM.HasIntermediateParent(newWM.handle, newWM.rootParentHandle);
            }

            // Add immediate children
            AddImmediateChildren(newWM);
        }

        private bool WindowViewIsAwareOfThisWindow(WindowMetadata newWM)
        {
            if (WindowPointersEnumerated.Contains(newWM.handle))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="possiblyCreatedObject"></param>
        /// <returns>True if the object was returned without being created.</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        private void GetAddRootObject(IntPtr handle, out WindowMetadata possiblyCreatedObject)
        {
            if (rootWindows.TryGetValue(handle, out WindowMetadata value))
            {
                possiblyCreatedObject = value;
                return;
            }
            possiblyCreatedObject = AddRootObject(handle);
        }

        private WindowMetadata AddRootObject(IntPtr aRootHandle)
        {
            var newRoot = new WindowMetadata(aRootHandle)
            {
                position = WindowsAPITools.GetWindowPosition(aRootHandle),
                size = WindowsAPITools.GetWindowSize(aRootHandle),
                ViewState = WindowsAPITools.GetWindowViewState(aRootHandle)
            };
            newRoot.HasTitle();
            rootWindows.Add(aRootHandle, newRoot);
            return newRoot;
        }
        
        /// <summary>
         /// 
         /// </summary>
         /// <param name="handle"></param>
         /// <param name="possiblyCreatedObject"></param>
         /// <returns>True if the object was returned without being created.</returns>
         /// <exception cref="KeyNotFoundException"></exception>
        private bool GetAddChildObject(IntPtr handleChild, WindowMetadata theParent, out WindowMetadata possiblyCreatedObject)
        {
            if (theParent.children == null)
            {
                theParent.children = new
                    Dictionary<IntPtr, WindowMetadata>();
            }
            if (theParent.children.TryGetValue(handleChild, out WindowMetadata value))
            {
                possiblyCreatedObject = value;
                return true;
            }
            possiblyCreatedObject = AddChildObject(handleChild, theParent);
            return true;
        }

        private WindowMetadata AddChildObject(IntPtr handle, WindowMetadata theParent)
        {
            var windowObjectCreation = new WindowMetadata(handle)
            {
                position = WindowsAPITools.GetWindowPosition(handle),
                size = WindowsAPITools.GetWindowSize(handle),
                ViewState = WindowsAPITools.GetWindowViewState(handle)
            };
            windowObjectCreation.HasTitle();

            theParent.children.Add(handle, windowObjectCreation);
            return windowObjectCreation;
        }

        private void AddImmediateChildren(WindowMetadata theParent)
        {
            foreach (var child in GetAllChildrenFromSystem(theParent.handle))
            {
                GetAddChildObject(child, theParent, out WindowMetadata _);
            }
        }

        private List<IntPtr> GetAllChildrenFromSystem(IntPtr hwnd)
        {
            var childPtr = hwnd;
            var childrenPtrs = new List<IntPtr>();
            while ((int)childPtr != 0)
            {
                if (childPtr != hwnd)
                {
                    childrenPtrs.Add(childPtr);
                    childPtr = WindowsAPITools.FindWindowExAWrapper(hwnd, childPtr, null, null);
                }
                else
                {
                    childPtr = WindowsAPITools.FindWindowExAWrapper(hwnd, IntPtr.Zero, null, null);
                }


            }
            return childrenPtrs;
        }
    }
}