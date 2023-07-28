using System.Collections.Generic;

namespace viewTools
{
    public class DataStructs
    {
        public static Dictionary<double, string> AspectRatio = new Dictionary<double, string>()
            {
                { 0, "Unknown" },
                { 0.1875, "3:16" },
                { 0.5625, "9:16" },
                { 0.6, "3:5" },
                { 0.6666, "2:3" },
                { 1, "1:1" },
                { 1.19, "19:16" },
                { 1.25, "5:4" },
                { 1.3333, "4:3" },
                { 1.375, "11:8" },
                { 1.43, "1p43:1" },
                { 1.5, "3:2" },
                { 1.56, "14:9" },
                { 1.6, "16:10" },
                { 1.618, "φ:1" },
                { 1.6666, "5:3" },
                { 1.7777, "16:9" },
                { 1.85, "1p85:1" },
                { 1.9, "1p9:1" },
                { 2, "2:1" },
                { 2.2, "2p2:1" },
                { 2.35, "2p35:1" },
                { 2.370370, "64:27" },
                { 2.39, "2p39:1" },
                { 2.4, "2p4:1" },
                { 2.414, "δS:1" },
                { 2.76, "2p76:1" },
                { 3.5, "32:9" },
                { 3.6, "18:5" },
                { 4, "4:1" }
            };

        public enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            MAXIMIZE = 3,
            SHOWNOACTIVATE = 4,
            SHOW = 5,
            MINIMIZE = 6,
            SHOWMINNOACTIVE = 7,
            SHOWNA = 8,
            RESTORE = 9,
            SHOWDEFAULT = 10,
            FORCEMINIMIZE = 11,
        }

        public static Dictionary<string, ViewRectangle> DisplayModelExternalMargins = new Dictionary<string, ViewRectangle>()
        {
            { "M422i-B1", new ViewRectangle(8, 40, 8, 40)},
            { "SE198WFP", new ViewRectangle(0, 0, 0, 0)}
        };
    }
}
