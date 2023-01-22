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
        }
        public void filterOutNonUserWindowObjects(WindowMetadata aProspectiveWindowObject)
        {
            
            // Candidates that are NOT viable are:
            //      Size is zero(0 for either or both height and width)
            //      Outside max bounds of a display.
            //      ViewState is set to hidden
            if (aProspectiveWindowObject.size == 0
                || aProspectiveWindowObject.ViewState == ShowWindowCommands.Hide.ToString()
                ||  
            {
                aProspectiveWindowObject.isViableWindow = false;
                return;
            }

            if (aProspectiveWindowObject.HasTitle())
            {
                aProspectiveWindowObject.isViableWindow = true;
            }
        }
    }
}
