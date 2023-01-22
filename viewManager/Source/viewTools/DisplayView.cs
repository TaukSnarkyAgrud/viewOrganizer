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

        public Rectangle maxBounds;

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
            CalculateMaxBounds();
        }

        private void CalculateMaxBounds()
        {
            var leftMost = displaysInView.Min(m => m.actualResolution.X);
            var topMost = displaysInView.Min(m => m.actualResolution.Y);
            var rightMost = displaysInView.Max(m => calculateRightDisplayBound(m));
            var bottomMost = displaysInView.Max(m => calculateBottomDisplayBound(m));
            maxBounds = new Rectangle(leftMost, bottomMost, rightMost, topMost);
            Debug.WriteLine($"Max Bounds: {maxBounds}");
        }

        private int calculateBottomDisplayBound(Display m)
        {
            if (m.externalMargin.IsEmpty)
            {
                return m.actualResolution.Y + m.actualResolution.Height;
            }
            return m.actualResolution.Y + m.externalMargin.Y + m.actualResolution.Height + m.externalMargin.Height;
        }

        private int calculateRightDisplayBound(Display m)
        {
            if (m.externalMargin.IsEmpty)
            {
                return m.actualResolution.X + m.actualResolution.Width;
            }
            return m.actualResolution.X + m.externalMargin.X + m.actualResolution.Width + m.externalMargin.Width;
        }
    }
}
