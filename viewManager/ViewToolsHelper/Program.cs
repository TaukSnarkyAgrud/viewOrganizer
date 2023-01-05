using viewTools;

namespace ViewToolsHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            var aObj = new Tools();
            aObj.PrintAllProcessesWithWindows();

            var testWindow = aObj.GetWindowProcessContains("Notepad");

            aObj.ConfigureWindowSizePosition(testWindow.MainWindowHandle, 0, 0);
        }
    }
}