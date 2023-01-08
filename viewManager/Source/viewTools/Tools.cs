using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace viewTools
{
    // https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features#size-and-position-messages

    //Get child windows
    //Get VS window and children
    //Get explorer windows
    //Identify ignore handles

    // implement "stapled" so that a window group is in the same zaxis
    // method for bring to top

    //Consider converting function calls to messages
    // https://learn.microsoft.com/en-us/windows/win32/winmsg/about-messages-and-message-queues#windows-messages

    // TODO: implement method to remove window titlebars

    //idea for identifying ghost windows: 
    //    See if left top right bottom is way out of screen bounds
    //    If all, top left right bottom are 0
    public class Tools
    {
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
                            if(prop.ToString().Contains("MainWindowTitle"))
                            {
                                Debug.Write($"{prop} = ");
                                Debug.WriteLine($"{prop.GetValue(proc)}");
                            }
                        }
                        catch (Exception e)
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

        public List<IntPtr> GetAllChildHandles(IntPtr hwnd)
        {
            return WindowsAPITools.GetAllChildHandles(hwnd);
        }
        public List<IntPtr> GetAllChildChromeWigets(IntPtr parent)
        {
            var anyWgtHdl = WindowsAPITools.GetAnyChromeWigetHandle();
            Debug.WriteLine(anyWgtHdl);
            var nxtHdl = WindowsAPITools.FindWindowExA(IntPtr.Zero, anyWgtHdl, "Chrome_WidgetWin_1", null);
            Debug.WriteLine(nxtHdl);
            return GetAllChildHandles(anyWgtHdl);
        }

        public void GetAllWindowObjects()
        {
            GetChromeWindows();
        }

        public void GetChromeWindows()
        {
            try
            {
                GetWindowProcessMatchProcessName("chrome");
                Debug.WriteLine("Chrome process found");
                WindowsAPITools.GetAllChromeWigetHandles();
            }
            catch (KeyNotFoundException ex)
            {
                Debug.WriteLine("No chrome windows found");
                Debug.WriteLine(ex.Message);
            }
        }
    }
}