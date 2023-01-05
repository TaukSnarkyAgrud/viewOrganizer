using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace viewTools
{
    public class Tools
    {
        public List<Process> GetAllProcessesWithWindows()
        {
            return Process.GetProcesses().Where(p => string.IsNullOrEmpty(p.MainWindowTitle) == false).ToList();
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

        public Process GetWindowProcessContains(string subString)
        {
            var possibleProcs = GetWindowProcessesContains(subString);
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

        public List<Process> GetWindowProcessesContains(string subString)
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

        //Get child windows
        //Get VS window and children
        //Get explorer windows
        //Get ignore handles
    }
}