using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace viewTools
{
    public class WindowMetadata
    {
        public IntPtr handle;
        public IntPtr title;
        public Process mainProcess;
        public List<WindowMetadata> children;
    }
}
