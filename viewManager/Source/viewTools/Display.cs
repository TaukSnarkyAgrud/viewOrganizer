using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using System.Windows.Forms;
using static viewTools.DataStructs;

namespace viewTools
{
    public class Display
    {
        public static List<Display> displaysAvailable;

        public Rectangle professedResolution;
        public Rectangle actualResolution;
        public string professedAspectRatio;
        public double professedAspectRatioScalar;
        public string actualAspectRatio;
        public double actualAspectRatioScalar;
        public Rectangle position;
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
            professedResolution = new Rectangle(sObject.WorkingArea.X, sObject.WorkingArea.Y, sObject.WorkingArea.Width, sObject.WorkingArea.Height);
            professedAspectRatioScalar = professedResolution.Width / professedResolution.Height;
            professedAspectRatio = ResolveRatioThruQuantize(professedAspectRatioScalar);

            if (externalMargin == null)
            {
                actualResolution = professedResolution;
                actualAspectRatio = professedAspectRatio;
            }
            else
            {
                actualResolution = RectangleUnion(professedResolution, externalMargin.rectangle);
                actualAspectRatioScalar = actualResolution.Width / actualResolution.Height;
                actualAspectRatio = ResolveRatioThruQuantize(actualAspectRatioScalar);
            }
        }

        private Rectangle RectangleUnion(Rectangle aRes, Rectangle aMargin)
        {
            return new Rectangle(aRes.X + aMargin.X, aRes.Y + aMargin.Y, aRes.Width + aMargin.Width, aRes.Height + aMargin.Height);
        }

        private string ResolveRatioThruQuantize(double someRatio)
        {
            if (Math.Floor(someRatio) >= 4) return DataStructs.AspectRatio[4];
            int n = DataStructs.AspectRatio.Values.Count;
            var keys = DataStructs.AspectRatio.Keys.ToArray();
            double previousKey = keys[n - 2];
            for (int i = 1; i < n; ++i)
            {
                double key = keys[i];
                int previousIndex = i - 1;
                previousKey = keys[previousIndex];
                int nextIndex = i + 1;

                if (nextIndex == n - 1)
                {
                    if (key > previousKey)
                    {
                        return DataStructs.AspectRatio[i];
                    }
                    return DataStructs.AspectRatio[previousKey];
                }

                if (key > previousKey && key < keys[nextIndex])
                {
                    var sP = key - previousKey;
                    var sN = keys[nextIndex] - key;

                    if (sP > sN)
                    {
                        return DataStructs.AspectRatio[keys[nextIndex]];
                    }
                }
            }
            return DataStructs.AspectRatio[previousKey];
        }

        private void calculateMaxBounds()
        {
            // check enum
            try
            {
                externalMargin = DataStructs.DisplayModelExternalMargins[sObject.DeviceName];
                return;
            }catch(NullReferenceException)
            {
                
            }
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
