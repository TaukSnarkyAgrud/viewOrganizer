using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace viewTools
{
    public class WindowMetadata
    {
        public IntPtr handle;
        public string title;
        public Process rootProcess;
        public int rootProcessId;
        public IntPtr rootParentHandle;
        public Dictionary<IntPtr, WindowMetadata> children;

        public ViewRectangle rectangle;
        public string ViewState;

        public bool hasIntermediateParent;
        public IntPtr immediateParentHandle;

        public bool isViableWindow = false;
        public bool isViableWebWindow = false;
        public string url;

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

        public StreamMetadata streamMetadata;


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
            rootProcess = mainProcess;
        }


        public override string ToString()
        {
            return $"Window Object: {GetStringOrPlaceholder(rootProcess?.ProcessName, "mainProcessName")}" +
                $" | {GetStringOrPlaceholder(handle.ToString(), nameof(handle))} 0x{handle.ToString("x8")}" +
                $" | {GetStringOrPlaceholder(title, nameof(title))}" +
                $" | {GetStringOrPlaceholder(rootProcess?.Id.ToString(), "mainProcessId")}" +
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


        public static IntPtr GetRootParent(IntPtr hwnd)
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

        public static bool IsRootParent(WindowMetadata aWM)
        {
            var rootPtr = GetRootParent(aWM.handle);
            if (rootPtr == aWM.handle)
            {
                return true;
            } else
            {
                aWM.rootParentHandle = rootPtr;
            }
            return false;
        }

        public static IntPtr GetImmediateParent(IntPtr hwnd)
        {
            return WindowsAPITools.GetParentWrapper(hwnd);
        }

        public static bool HasIntermediateParent(WindowMetadata aWM)
        {
            if (aWM.rootParentHandle == (aWM.immediateParentHandle = GetImmediateParent(aWM.handle)))
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
            WindowsAPITools.GetWindowTextWrapper(handle, out string titleString, 100);
            title = titleString;

            // Check if the title was set
            if (title != null && title.Count() > 0)
            {
                return true;
            }
            return false;
        }

        internal bool OutSideDisplayView()
        {
            var intersects = false;
            var correction = DisplayView.workingAreaPositiveCorrection();
            List<Display> displays = DisplayView.displaysInView;
            var theWindowRectange = rectangle.rectangle;
            if (!DisplayView.workingAreaPositiveCorrection().IsEmpty)
            {
                displays = new List<Display>();
                foreach ( var dvD in DisplayView.displaysInView )
                {
                    var positionCorrected = CorrectPosition(dvD.position, correction);
                    displays.Add(new Display(positionCorrected, dvD.actualResolution));
                }
                var thisRect = rectangle.rectangle;
                theWindowRectange = new Rectangle(thisRect.X + correction.X, thisRect.Y + correction.Y, thisRect.Width, thisRect.Height);
            }
            foreach (var display in displays)
            {
                if (display.sObject.WorkingArea.IntersectsWith(theWindowRectange))
                {
                    intersects= true;
                }
            }
            if (!intersects)
            {
                Debug.WriteLine($"{handle}Doesn't intersect any displays");
            }
            return !intersects;
        }

        private ViewPosition CorrectPosition(ViewPosition position, Point correction)
        {
            return new ViewPosition(position.left + correction.X, position.top + correction.Y);
        }

        private bool RectangleIntersection(ViewRectangle maxBounds, ViewRectangle rectangle)
        {
            var translateBoundRectToPositive = maxBounds.TranslateRectangleToPositive();
            var translateWindowRectToPositive = rectangle.TranslateRectangleToPositive();
            return translateBoundRectToPositive.IntersectsWith(translateWindowRectToPositive);
        }

        public class StreamMetadata
        {
            public bool isStreamWindow;
            public bool isChatWindow;
            public string hostSite;
            public string streamerTag;

            public static List<string> hostSites = new()
            {
                "facebook",
                "youtube",
                "twitch"
            };

            public static Dictionary<string, Dictionary<string, string>> streamerTags = new()
            {
                { "poolshark", new(){ { "twitch", "thepoolshark" },{ "youtube", "thepoolshark" },{ "facebook", "PoolsharkGaming" } } },
                { "lupo", new(){ { "youtube", "DrLupo" } } },
                { "trip", new(){ { "twitch", "triple_g" } } },
                { "pestily", new(){ { "twitch", "pestily" } } },
                { "fab", new(){ { "youtube", "NotFabTV" }, { "twitch", "notfabtv" } } },
                { "paul", new(){ { "twitch", "actionjaxon" } } },
                { "jen", new(){ { "twitch", "jenntacles" } } },
                { "aims", new(){ { "twitch", "aims" } } },
                { "fudge", new(){ { "facebook", "fudgexl" } } },
                { "tim", new(){ { "facebook", "Darkness429" } } },
                { "tweety", new(){ { "twitch", "tweetyexpert" } } },
                { "hodsey", new(){ { "twitch", "hodsy" } } },
                { "bearki", new(){ { "twitch", "bearki" } } },
                { "AnneMunition", new(){ { "twitch", "AnneMunition" } } },
                { "mr___meme", new(){ { "youtube", "mr___meme" } } },
                { "cali", new(){ { "youtube", "caliverse" } } },
                { "clintus", new(){ { "youtube", "clintus" } } },
                { "bull", new(){ { "facebook", "Bull1060" } } },
                { "elliot", new(){ { "facebook", "ElliottAsAlways" } } },
                { "mugs", new(){ { "facebook", "MugsTV" } } },
                { "call", new(){ { "facebook", "Callofcrafters0" } } },
                { "josh", new(){ { "youtube", "theJOSHfeed" } } },

            };

            public static void defineIdentity(WindowMetadata window)
            {
                // chrome(or other web browser in future) window but not normal window only app type webbrowser
                // has any site name, streamer name, or significant string in its metadata
                // TODO: write more ideas on identifying window attributes here
                // If developer can discern a method of naming a window in inception with a unique id or attributes

                defineWindowCandidacyByProcess(window);
                defineWindowCandidacyByURL(window);
                defineIdentityByWindowTitle(window);
            }

            private static void defineWindowCandidacyByURL(WindowMetadata window)
            {
                
            }

            private static void defineWindowCandidacyByProcess(WindowMetadata window)
            {
                //if(window.rootProcess.ProcessName == "chrome")
                //{
                    
                //    window.streamMetadata.isStreamWindow = false;
                //}
            }

            private static void defineIdentityByWindowTitle(WindowMetadata window)
            {
                foreach (var site in StreamMetadata.hostSites)
                {
                    if (window.title.ToLower().Contains(site))
                    {
                        InitializeStreamMetadata(window);
                        var siteText = window.streamMetadata.hostSite;
                        if (string.IsNullOrEmpty(siteText))
                        {
                            window.streamMetadata.hostSite = site;
                        }
                        else
                        {
                            window.streamMetadata.hostSite = siteText + " " + site;
                        }
                    }
                }

                foreach (var tag in getAllTags())
                {
                    if (window.title.ToLower().Contains(tag))
                    {
                        InitializeStreamMetadata(window);
                        var tagText = window.streamMetadata.streamerTag;
                        if (string.IsNullOrEmpty(tagText))
                        {
                            window.streamMetadata.streamerTag = tag;
                        }
                        else
                        {
                            window.streamMetadata.streamerTag = tagText + " " + tag;
                        }
                    }
                }
            }

            private static List<string> getAllTags()
            {
                var tags = new List<string>();
                foreach (var streamer in streamerTags)
                {
                    foreach (var tg in streamer.Value.Values)
                    {
                        tags.Add(tg);
                    }
                }
                return tags;
            }

            private static void InitializeStreamMetadata(WindowMetadata aWindow)
            {
                aWindow.streamMetadata ??= new StreamMetadata();
            }
        }
    }
}
