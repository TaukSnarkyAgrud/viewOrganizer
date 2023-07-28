using System.Diagnostics;

namespace viewTools
{
    // https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features#size-and-position-messages

    //Get child windows
    //Get VS window and children
    //Get explorer windows
    //Identify ignore handles

    // implement "stapled" so that a window group is in the same zaxis
    // method for bring to top

    //Consider converting function calls to messages
    // https://learn.microsoft.com/en-us/windows/win32/winmsg/about-messages-and-message-queues#windows-messages

    // TODO: implement method to remove window titlebars

    //idea for identifying ghost windows: 
    //    See if left top right bottom is way out of screen bounds
    //    If all, top left right bottom are 0
    public class Tools
    {
        View aView;
        public void CreateView()
        {
            aView = new View();

            foreach (var dis in DisplayView.displaysInView)
            {
                Debug.WriteLine(dis);
            }
        }
    }
}