using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace viewTools
{
    public class Display
    {
        public static List<Display> displaysAvailable;

        public ViewRectangle professedResolution;
        public ViewRectangle actualResolution;
        public string professedAspectRatio;
        public double professedAspectRatioScalar;
        public string actualAspectRatio;
        public double actualAspectRatioScalar;
        public ViewPosition position;
        public ViewRectangle externalMargin;

        public string displayName;

        public Screen sObject;

        public Display(Screen display)
        {
            sObject = display;
            displayName = sObject.DeviceName;
            calculateMaxBounds();
            workingArea();
        }

        public Display(ViewPosition position, ViewRectangle actualResolution)
        {
            this.position = position;
            this.actualResolution = actualResolution;
        }

        public override string ToString()
        {
            return $"Display Object: DisplayName: {displayName}{sObject.DeviceName}\n" +
                $"position: {position}\n" +
                $"externalMargin: {externalMargin}\n" +
                $"professedResolution: {professedResolution}\n" +
                $"professedAspectRatio: {professedAspectRatio}\n" +
                $"professedAspectRatioScalar: {professedAspectRatioScalar}\n" +
                $"actualResolution: {actualResolution}\n" +
                $"actualAspectRatio: {actualAspectRatio}\n" +
                $"actualAspectRatioScalar: {actualAspectRatioScalar}\n";
        }

        private void workingArea()
        {
            professedResolution = new ViewRectangle(sObject.WorkingArea.X, sObject.WorkingArea.Y, new Size(sObject.WorkingArea.Width, sObject.WorkingArea.Height));
            professedAspectRatioScalar = (double)professedResolution.width / (double)professedResolution.height;
            professedAspectRatio = ResolveRatioThruQuantize(professedAspectRatioScalar);

            if (externalMargin == null || externalMargin.rectangle.IsEmpty)
            {
                actualResolution = professedResolution;
                actualAspectRatio = professedAspectRatio;
            }
            else
            {
                actualResolution = RectangleUnion(professedResolution, externalMargin);
                actualAspectRatioScalar = (double)actualResolution.width / (double)actualResolution.height;
                actualAspectRatio = ResolveRatioThruQuantize(actualAspectRatioScalar);
            }
        }

        private ViewRectangle RectangleUnion(ViewRectangle aRes, ViewRectangle aMargin)
        {
            return new ViewRectangle(aRes.left + aMargin.left, aRes.top + aMargin.top, aRes.width + aMargin.width, aRes.height + aMargin.height);
        }

        private string ResolveRatioThruQuantize(double someRatio)
        {
            if (Math.Floor(someRatio) >= 4) return DataStructs.AspectRatio[4];
            int n = DataStructs.AspectRatio.Values.Count;
            var keys = DataStructs.AspectRatio.Keys.ToArray();
            double previousKey = keys[n-2];
            for (int i = 1; i < n; ++i)
            {
                int previousIndex = i - 1;
                int nextIndex = i + 1;
                double key = keys[i];
                previousKey = keys[previousIndex];
                double nextKey = keys[nextIndex];

                if (nextIndex == n - 1)
                {
                    if (someRatio > previousKey)
                    {
                        return DataStructs.AspectRatio[i];
                    }
                    return DataStructs.AspectRatio[previousKey];
                }

                if (someRatio == previousKey)
                {
                    return DataStructs.AspectRatio[previousKey];
                }

                if (someRatio == key)
                {
                    return DataStructs.AspectRatio[key];
                }

                if (someRatio == nextKey)
                {
                    return DataStructs.AspectRatio[nextKey];
                }

                if (someRatio < previousKey)
                {
                    return DataStructs.AspectRatio[previousKey];
                }

                if (someRatio < key)
                {
                    var sP = someRatio - previousKey;
                    var sC = key - someRatio;

                    if (sC > sP)
                    {
                        return DataStructs.AspectRatio[previousKey];
                    } else
                    {
                        return DataStructs.AspectRatio[key];
                    }
                }

                if (someRatio < nextKey)
                {
                    var sN = nextKey - someRatio;
                    var sC = someRatio - key;

                    if (sC > sN)
                    {
                        return DataStructs.AspectRatio[nextKey];
                    }
                    else
                    {
                        return DataStructs.AspectRatio[key];
                    }
                }
            }
            return DataStructs.AspectRatio[previousKey];
        }

        private void calculateMaxBounds()
        {
            // check enum
            //try
            //{
            //    externalMargin = DataStructs.DisplayModelExternalMargins[sObject.DeviceName];
            //    return;
            //}catch(NullReferenceException)
            //{
                
            //}
            externalMargin = new ViewRectangle(0, 0, 0, 0);
        }

        //private void monitorQuery()
        //{
        //    SelectQuery Sq = new SelectQuery("Win32_DesktopMonitor");
        //    ManagementObjectSearcher objOSDetails = new ManagementObjectSearcher(Sq);
        //    ManagementObjectCollection osDetailsCollection = objOSDetails.Get();
        //    StringBuilder sb = new StringBuilder();
        //    foreach (ManagementObject mo in osDetailsCollection)
        //    {
        //        sb.AppendLine(string.Format("Name : {0}", (string)mo["Name"]));
        //        sb.AppendLine(string.Format("Availability: {0}", (ushort)mo["Availability"]));
        //        sb.AppendLine(string.Format("Caption: {0}", (string)mo["Caption"]));
        //        sb.AppendLine(string.Format("InstallDate: {0}", Convert.ToDateTime(mo["InstallDate"]).ToString()));
        //        sb.AppendLine(string.Format("ConfigManagerUserConfig: {0}", mo["ConfigManagerUserConfig"].ToString()));
        //        sb.AppendLine(string.Format("CreationClassName : {0}", (string)mo["CreationClassName"]));
        //        sb.AppendLine(string.Format("Description: {0}", (string)mo["Description"]));
        //        sb.AppendLine(string.Format("DeviceID : {0}", (string)mo["DeviceID"]));
        //        sb.AppendLine(string.Format("ErrorCleared: {0}", (string)mo["ErrorCleared"]));
        //        sb.AppendLine(string.Format("ErrorDescription : {0}", (string)mo["ErrorDescription"]));
        //        sb.AppendLine(string.Format("ConfigManagerUserConfig: {0}", mo["ConfigManagerUserConfig"].ToString()));
        //        sb.AppendLine(string.Format("LastErrorCode : {0}", mo["LastErrorCode"]).ToString());
        //        sb.AppendLine(string.Format("MonitorManufacturer : {0}", mo["MonitorManufacturer"]).ToString());
        //        sb.AppendLine(string.Format("PNPDeviceID: {0}", (string)mo["PNPDeviceID"]));
        //        sb.AppendLine(string.Format("MonitorType: {0}", (string)mo["MonitorType"]));
        //        sb.AppendLine(string.Format("PixelsPerXLogicalInch : {0}", mo["PixelsPerXLogicalInch"].ToString()));
        //        sb.AppendLine(string.Format("PixelsPerYLogicalInch: {0}", mo["PixelsPerYLogicalInch"].ToString()));
        //        sb.AppendLine(string.Format("ScreenHeight: {0}", mo["ScreenHeight"].ToString()));
        //        sb.AppendLine(string.Format("ScreenWidth : {0}", mo["ScreenWidth"]).ToString());
        //        sb.AppendLine(string.Format("Status : {0}", (string)mo["Status"]));
        //        sb.AppendLine(string.Format("SystemCreationClassName : {0}", (string)mo["SystemCreationClassName"]));
        //        sb.AppendLine(string.Format("SystemName: {0}", (string)mo["SystemName"]));
        //    }
        //}
    }
}
