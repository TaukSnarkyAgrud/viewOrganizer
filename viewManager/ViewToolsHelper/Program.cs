using viewTools;

namespace ViewToolsHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            var aObj = new Tools();
            aObj.PrintAllProcessesWithWindows();

            var testHandle = aObj.GetWindowHandleMatchTitleWord("Notepad");

            Console.WriteLine(testHandle);

            aObj.BringWindowTop(testHandle);
            aObj.RemoveTitleBar(testHandle);
        }
    }
}