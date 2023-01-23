using System;
using System.Collections.Generic;
using System.Drawing;
using static viewTools.DataStructs;

namespace viewTools
{
    public class View
    {
        public static List<DisplayView> dViews;
        public DisplayView dView;
        public WindowView wView;
        public View()
        {
            dViews = new List<DisplayView>();
            dView = new DisplayView();
            dViews.Add(dView);
            wView = new WindowView(dView);
            filterOutNonUserWindowObjects();
        }
        public void filterOutNonUserWindowObjects()
        {
            // Candidates that are NOT viable are:
            //      Size is zero(0 for either or both height and width)
            //      Outside max bounds of a display.
            //      ViewState is set to hidden
            foreach (var window in wView.AllWindowObjectsEnumerated)
            {
                // Disqualifying attributes
                if (window.size == 0
                || window.ViewState == ShowWindowCommands.Hide.ToString()
                || (window.OutSideDisplayView() && window.ViewState != ShowWindowCommands.Minimized.ToString()))
                {
                    window.isViableWindow = false;
                }

                // Qualifying attributes
                if (window.HasTitle())
                {
                    window.isViableWindow = true;
                }
            }
        }
    }
}
