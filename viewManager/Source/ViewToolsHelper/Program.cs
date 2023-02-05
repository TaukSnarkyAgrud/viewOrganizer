using System.Diagnostics;

namespace ViewToolsHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            var aObj = new viewTools.Tools();
            var breaker = "----------------------------------------------------------------------------------------------------------------------------------------";
            Debug.WriteLine($"{breaker}START");
            aObj.CreateView();
            Debug.WriteLine($"{breaker}END");
        }
    }
}