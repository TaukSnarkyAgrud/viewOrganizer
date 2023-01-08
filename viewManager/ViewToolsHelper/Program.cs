using System.Diagnostics;
using viewTools;

namespace ViewToolsHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            var aObj = new Tools();

            aObj.GetChromeWindows();
            Debug.WriteLine("Window Object {handle} {title} {mainProcess} {children} {position} {size} {ViewState} {hasIntermediateParent} {immediateParentHandle} {isViableWindow}");
            Debug.WriteLine("-------------------------------------------------------------------------------------------------------------------------------------------------------------------------");
            foreach (var item in WindowMetadata.RootWindows.Values.ToList<WindowMetadata>())
            {
                Debug.WriteLine(item);
                if (item.children != null)
                {
                    foreach (var child in item.children.Values.ToList<WindowMetadata>())
                    {
                        Debug.WriteLine(child);
                    }
                }
            }
        }
    }
}