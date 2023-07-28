using System;
using System.Collections.Generic;

namespace viewTools
{
    class StreamingView
    {
        private Dictionary<IntPtr, WindowMetadata> StreamWindows;
        public StreamingView(List<WindowMetadata> viableWindows)
        {
            StreamWindows ??= new();

            AddToStreamWindowList(viableWindows);
        }

        private void AddToStreamWindowList(List<WindowMetadata> viableWindows)
        {
            foreach (var wndPtr in viableWindows)
            {
                if (WindowIsViableStreamWindow(wndPtr))
                {
                    try
                    {
                        StreamWindows.Add(wndPtr.handle, wndPtr);
                    }
                    catch
                    {
                        // Swallow add failure silently
                    }
                }
            }
        }

        private bool WindowIsViableStreamWindow(WindowMetadata wndPtr)
        {
            if(wndPtr.streamMetadata != null)
            {
                return true;
            }
            return false;
        }
    }
}
