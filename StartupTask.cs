using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Push;
using Windows.System.Threading;
using Windows.ApplicationModel.Activation;


//Added trusted client for PowerShell: https://docs.microsoft.com/en-us/windows/iot-core/connect-your-device/powershell: Set-Item WSMan:\localhost\Client\TrustedHosts -Value
//look here for device management: https://docs.microsoft.com/en-us/windows/iot-core/manage-your-device/azureiotdm
//reading iBeacons: https://sandervandevelde.wordpress.com/2016/03/28/reading-ibeacons-using-a-uwp-app-on-your-raspberry-pi/ 
// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409
//good example: https://sandervandevelde.wordpress.com/2016/04/08/building-a-windows-10-iot-core-background-webserver/
//https://blog.mikejmcguire.com/2016/03/07/getting-started-with-a-windows-10-iot-background-application-on-the-raspberry-pi-3/

namespace TrainSpotter
{
    public sealed class StartupTask : IBackgroundTask
    {
        private static BackgroundTaskDeferral _Deferral = null;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {

            _Deferral = taskInstance.GetDeferral();

            AppCenter.LogLevel = LogLevel.Verbose;
            AppCenter.Start("xxxxxx-xxxxxxx", typeof(Analytics));    //for AppCenter

   
            Analytics.SetEnabledAsync(true);
            AppCenter.LogLevel = LogLevel.Verbose;


            var beaconTasks = new RadBeaconReader();
            Analytics.TrackEvent("Launch Beacon Watcher", new Dictionary<string, string> {
               { "Category", "Beacon" },
               { "Function", "StartupTask.Run"}
            });
            await ThreadPool.RunAsync(workItem =>
            {
                beaconTasks.StartBeaconManager();
            });

        }
    


    }
}
