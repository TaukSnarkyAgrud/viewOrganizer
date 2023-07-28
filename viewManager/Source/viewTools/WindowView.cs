using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Linq;
using static viewTools.DataStructs;
using ChromeTools;
using System.Threading.Tasks;

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

        private List<StreamingView> streamViews;
        public Dictionary<string, string> handleUrls;

        private Dictionary<string, List<string>> ChromeHandlesToUrls;

        public List<IntPtr> WindowPointersEnumerated {
            get
            {
                if (rootWindows == null) { return new List<IntPtr>(); }
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
                windowPointersEnumerated = items;
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
        public List<WindowMetadata> AllViableWindowObjectsEnumerated
        {
            get
            {
                // Collect Chrome url-handle pairs for use below
                //handleUrls = CollectChromeUrlHandlePairs();

                // Viability can be filtered out here for a few data points
                AllWindowObjectsEnumerated.ForEach(w => {
                    // Check for sequential filter in criteria
                    w.isViableWindow = WindowsAPITools.IsAltTabWindow(w.handle);

                    // Check for sequential filter out criteria
                    if (w.size == 0
                    || w.ViewState == ShowWindowCommands.Hide.ToString()
                    || (w.OutSideDisplayView() && w.ViewState != ShowWindowCommands.Minimized.ToString())
                    || !w.HasTitle())
                    {
                        w.isViableWindow = false;
                    }

                    if (w.isViableWindow)
                    {
                        try
                        {
                            w.rootProcess = Process.GetProcessById(w.rootProcessId);
                        }
                        catch (System.ArgumentException)
                        {
                            w.isViableWindow = false;
                        }
                    }

                    //if (handleUrls.TryGetValue(w.handle.ToString(), out var matchUrl))
                    //{
                    //    w.isViableWebWindow = true;
                    //    w.url = matchUrl;
                    //}
                });

                return AllWindowObjectsEnumerated.Where(x => x.isViableWindow).ToList<WindowMetadata>();
            }
        }

        private void CollectChromeUrlHandlePairs()
        {
            var chromeHandler = new ChromeApiHelper();
            var reply = chromeHandler.SendMessage("collectUrlsAndHandles");
            ParseReplyIntoUrls(reply);
        }

        private void ParseReplyIntoUrls(Task<string> reply)
        {
            this.ChromeHandlesToUrls = new();
        }

        public List<WindowMetadata> AllNonViableWindowObjectsEnumerated
        {
            get
            {
                return AllWindowObjectsEnumerated.Where(x => !x.isViableWindow).ToList<WindowMetadata>();
            }
        }
        public List<WindowMetadata> AllImmediateChildrenOfRootObjects
        {
            get
            {
                var firstChildren = new List<WindowMetadata>();
                foreach (var rwin in rootWindows)
                {
                    if (rwin.Value.children != null)
                    {
                        firstChildren.AddRange(rwin.Value.children.Values);
                    }
                }
                return firstChildren;
            }
        }

        // Showing no second children. possibly wrong
        public List<WindowMetadata> AllSecondChildren
        {
            get
            {
                var secondChilds = new List<WindowMetadata>();
                foreach (var fChild in AllImmediateChildrenOfRootObjects)
                {
                    if (fChild.children != null)
                    {
                        secondChilds.AddRange(fChild.children.Values.ToList<WindowMetadata>());
                    }
                }
                return secondChilds;
            }
        }


        public WindowView(DisplayView view)
        {
            dView = view;
            IngestAllWindowObjects();

            // Prints manifest
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

            streamViews ??= new();

            streamViews.Add(item: new StreamingView(AllViableWindowObjectsEnumerated));
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
                    items.AddRange(GetAllChildren(aWindow.children[child]));
                }
            }
            return items;
        }

        public void IngestAllWindowObjects()
        {
            IngestWindowsByAPI();
        }

        private void IngestWindowsByAPI()
        {
            windowPointersFromApiEnumerated = new List<IntPtr>();
            WindowsAPITools.EnumWindows(EnumWindowsCallback, 0);
            foreach (var item in windowPointersFromApiEnumerated)
            {
                GetAddWindow(new WindowMetadata(item));
            }

            foreach (var windowItem in AllViableWindowObjectsEnumerated)
            {
                WindowMetadata.StreamMetadata.defineIdentity(windowItem);
            }
        }

        public static bool EnumWindowsCallback(IntPtr hwnd, int lParam)
        {
            WindowView.windowPointersFromApiEnumerated.Add(hwnd);
            return true;
        }

        private void GetAddWindow(WindowMetadata newWM)
        {
            newWM.immediateParentHandle = WindowsAPITools.GetParentWrapper(newWM.handle);

            // If it already exists, ignore
            if (WindowViewIsAwareOfThisWindow(newWM))
            {
                return;
            }

            if (WindowMetadata.IsRootParent(newWM))
            {
                // Add this as a root
                GetAddRootObject(newWM.handle, out newWM);
            }
            else
            {
                // Its a child of some parent, add its root
                GetAddWindow(new WindowMetadata(newWM.rootParentHandle));
                return;
            }

            // Add immediate children
            AddImmediateChildren(newWM);
        }

        private bool WindowViewIsAwareOfThisWindow(WindowMetadata newWM)
        {
            return WindowViewIsAwareOfThisWindow(newWM.handle);
        }

        private bool WindowViewIsAwareOfThisWindow(IntPtr newWM)
        {
            if (WindowPointersEnumerated.Contains(newWM))
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
            rootWindows ??= new
                    Dictionary<IntPtr, WindowMetadata>();
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
                ViewState = WindowsAPITools.GetWindowViewState(aRootHandle),
                rootProcessId = GetWindowProcessId(aRootHandle)
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
         /// <exception cref="KeyNotFoundException"></exception>
        private bool GetAddChildObject(IntPtr handleChild, WindowMetadata theParent, out WindowMetadata possiblyCreatedObject)
        {
            theParent.children ??= new
                    Dictionary<IntPtr, WindowMetadata>();
            if (theParent.children.TryGetValue(handleChild, out WindowMetadata value))
            {
                possiblyCreatedObject = value;
                return false;
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
                ViewState = WindowsAPITools.GetWindowViewState(handle),
                rootProcessId = GetWindowProcessId(handle),
                rootParentHandle = WindowMetadata.GetRootParent(handle)
            };
            windowObjectCreation.hasIntermediateParent = WindowMetadata.HasIntermediateParent(windowObjectCreation);
            windowObjectCreation.HasTitle();

            theParent.children.Add(handle, windowObjectCreation);
            return windowObjectCreation;
        }

        private int GetWindowProcessId(IntPtr handle)
        {
            // out pID is redudant, discard
            var theThreadId = WindowsAPITools.GetWindowThreadProcessId(handle, out int theProcessId);
            return theProcessId;
        }

        private void AddImmediateChildren(WindowMetadata theParent)
        {
            foreach (var child in GetAllImmediateChildrenFromSystem(theParent.handle))
            {
                GetAddChildObject(child, theParent, out WindowMetadata infant);
                AddImmediateChildren(infant);
            }
        }

        private List<IntPtr> GetAllImmediateChildrenFromSystem(IntPtr hwnd)
        {
            var childPtr = hwnd;
            var childrenPtrs = new List<IntPtr>();
            while ((int)childPtr != 0)
            {
                if (childPtr != hwnd)
                {
                    if (WindowsAPITools.GetParentWrapper(childPtr) == hwnd)
                    {
                        childrenPtrs.Add(childPtr);
                    }
                    childPtr = WindowsAPITools.FindWindowExAWrapper(hwnd, childPtr, null, null);
                }
                else
                {
                    childPtr = WindowsAPITools.FindWindowExAWrapper(hwnd, IntPtr.Zero, null, null);
                }
            }

            return childrenPtrs;
        }






        //public void RemoveTitleBar(IntPtr hwnd)
        //{
        //    WindowsAPITools.RemoveTitlebar(hwnd);
        //}

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

        public List<Process> GetAllProcessesWithWindows()
        {
            return Process.GetProcesses().Where(p => (int)p.MainWindowHandle != 0).ToList();
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

        private void IngestWindowsByProcess()
        {

            Debug.WriteLine($"\nOther Processes with handles found\n---------------------------------");
            var windowProcesses = GetAllProcessesWithWindows();
            foreach (var item in windowProcesses)
            {
                var alreadyAware = AllWindowObjectsEnumerated.FirstOrDefault(x => x.handle == item.MainWindowHandle);
                if (alreadyAware != null)
                {
                    alreadyAware.rootProcess = item;
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

        public IntPtr GetWindowHandleMatchTitleWord(string subString)
        {
            return GetWindowProcessMatchTitleWord(subString).MainWindowHandle;
        }

        public IntPtr GetWindowHandleMatchProcessName(string subString)
        {
            return GetWindowProcessMatchProcessName(subString).MainWindowHandle;
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
    }
}