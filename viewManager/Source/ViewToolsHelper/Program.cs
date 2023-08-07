using ChromeTools;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ViewToolsHelper
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var chHelper = new ChromeApiHelper();
            _ = chHelper.ListenForUpdates();
            while (true)
            {
                await Task.Delay(1000);
                string aMess;
                if (chHelper.PopMessage(out aMess))
                {
                    Console.WriteLine(aMess);
                }
            }
            //var aObj = new viewTools.Tools();
            //var breaker = "----------------------------------------------------------------------------------------------------------------------------------------";
            //Debug.WriteLine($"{breaker}START");
            //aObj.CreateView();
            //Debug.WriteLine($"{breaker}END");
        }
    }
}