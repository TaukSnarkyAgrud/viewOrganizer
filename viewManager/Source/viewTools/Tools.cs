using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace viewTools
{
    public class Tools
    {
        public List<WindowMetadata> windows_KnownActive;
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
                    Console.WriteLine($"------------------------------------------------------------------ Process {procnum} ({proc.Id})------------------------------------------------------------------\n");
                    foreach (var prop in proc.GetType().GetProperties())
                    {
                        try
                        {
                            if(prop.ToString().Contains("MainWindowTitle"))
                            {
                                Console.Write($"{prop} = ");
                                Console.WriteLine($"{prop.GetValue(proc)}");
                            }
                        }
                        catch (Exception e)
                        {

                            Console.WriteLine("exception thrown");
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

        public IntPtr GetWindowHandleMatchTitleWord(string subString)
        {
            return GetWindowProcessMatchTitleWord(subString).MainWindowHandle;
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

        public void ConfigureWindowSizePosition(IntPtr hwnd, int x, int y)
        {
            WindowsAPITools.ConfigureWindowSizePosition(hwnd, x, y);
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

    }
}