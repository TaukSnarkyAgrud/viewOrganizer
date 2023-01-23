using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace viewTools
{
    public class DisplayView
    {
        public static List<Display> displaysInView;

        public ViewRectangle maxBounds;

        public DisplayView()
        {
            GetAvailableDisplays();
        }

        private void GetAvailableDisplays()
        {
            displaysInView = new List<Display>();
            var displays = Screen.AllScreens;
            foreach (var display in displays)
            {
                var createDisplay = new Display(display);
                DisplayView.displaysInView.Add(createDisplay);
            }
        }

        public static Point workingAreaPositiveCorrection()
        {
            var leftMost = displaysInView.Min(d => d.position == null ? 0 : d.position.left);
            var topMost = displaysInView.Min(d => d.position == null ? 0 : d.position.top);
            if (leftMost >= 0 && topMost >= 0)
            {
                return new Point(0, 0);
            }
            return new Point(Math.Abs(leftMost), Math.Abs(topMost));
        }
        private void CalculateMaxDisplayViewBounds()
        {
            var leftMost = displaysInView.Min(m => m.actualResolution.left);
            var topMost = displaysInView.Min(m => m.actualResolution.top);
            var rightMost = displaysInView.Max(m => calculateRightDisplayBound(m));
            var bottomMost = displaysInView.Max(m => calculateBottomDisplayBound(m));
            maxBounds = new ViewRectangle(leftMost, topMost, rightMost, bottomMost);
            Debug.WriteLine($"Max Bounds: {maxBounds}");
        }

        private int calculateBottomDisplayBound(Display m)
        {
            return m.actualResolution.height + m.actualResolution.position.top;
        }

        private int calculateRightDisplayBound(Display m)
        {
            return m.actualResolution.width + m.actualResolution.position.left;
        }
    }
}
