using System.Diagnostics;
using viewTools;

namespace ViewToolsHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            var aObj = new Tools();
            var breaker = "----------------------------------------------------------------------------------------------------------------------------------------";
            Debug.WriteLine($"{breaker}START");
            aObj.CreateView();
            Debug.WriteLine($"{breaker}END");
        }
    }
}