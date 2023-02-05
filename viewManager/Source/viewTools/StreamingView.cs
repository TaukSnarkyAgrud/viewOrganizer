using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace viewTools
{
    class StreamingView
    {
        private Dictionary<IntPtr, WindowMetadata> StreamWindows;
        public StreamingView(List<WindowMetadata> viableWindows)
        {
            if (StreamWindows == null)
            {
                StreamWindows = new();
            }

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
            var isViableWindow = false;

            if (wndPtr.title.Contains("Twitch")
                || (wndPtr.title.ToLower().Contains("youtube") && !wndPtr.title.ToLower().Contains("google chrome"))
                || wndPtr.title.Contains("Facebook"))
            {
                isViableWindow = true;
            }

            return isViableWindow;
        }
    }
}
