﻿using ChromeTools;
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
            while (true)
            {
                var response = await chHelper.SendMessage("collectUrlsAndHandles");
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
            //var aObj = new viewTools.Tools();
            //var breaker = "----------------------------------------------------------------------------------------------------------------------------------------";
            //Debug.WriteLine($"{breaker}START");
            //aObj.CreateView();
            //Debug.WriteLine($"{breaker}END");
        }
    }
}