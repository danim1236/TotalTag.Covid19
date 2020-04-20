using System;
using TotalTag.Helpers;
using TotalTag.WebserviceClient.CoreClient;

namespace TotalTag.GateController.Helpers
{
    public class CommonHelper
    {
        public static void Init(bool devMode = false)
        {
            Console.WriteLine(
                $"{new VersionHelper().GetName()} V: {new VersionHelper().GetVersionText()} / TotalTag Lib V: {new VersionHelper().GetTotalTagVersionText()}");

            TotalTagClient.DevMode = devMode;
            ClientTagHelper.Init();
            UserClient.SetDefaultUser();
            Console.WriteLine($"Url: {TotalTagClient.StaticBaseUrl}:{TotalTagClient.StaticPort}");
        }
    }
}