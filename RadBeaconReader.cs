using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalBeacon.Library.Core.Entities;
using UniversalBeacon.Library.Core.Interop;
using UniversalBeacon.Library.UWP;
using System.Diagnostics;
using System.IO;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;

//using System.Runtime.InteropServices;
//using System.Threading;

namespace TrainSpotter
{


    sealed public class RadBeaconReader
    {

        //private WindowsBluetoothPacketProvider _provider;
        private BeaconManager _beaconManager;

        private bool _restartingBeaconWatch;

        //private struct BLUETOOTH_FIND_RADIO_PARAM
        //{
        //    internal UInt32 dwSize;
        //    internal void Initialize()
        //    {
        //        this.dwSize = (UInt32)Marshal.SizeOf(typeof(BLUETOOTH_FIND_RADIO_PARAM));
        //    }
        //}
        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="handle">[In] A valid handle to an open object.</param>
        ///// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error information, call GetLastError.</returns>
        //[DllImport("Kernel32.dll", SetLastError = true)]
        //static extern bool CloseHandle(IntPtr handle);

        /// <summary>
        /// Finds the first bluetooth radio present in device manager
        /// </summary>
        /// <param name="pbtfrp">Pointer to a BLUETOOTH_FIND_RADIO_PARAMS structure</param>
        /// <param name="phRadio">Pointer to where the first enumerated radio handle will be returned. When no longer needed, this handle must be closed via CloseHandle.</param>
        /// <returns>In addition to the handle indicated by phRadio, calling this function will also create a HBLUETOOTH_RADIO_FIND handle for use with the BluetoothFindNextRadio function.
        /// When this handle is no longer needed, it must be closed via the BluetoothFindRadioClose.
        /// Returns NULL upon failure. Call the GetLastError function for more information on the error. The following table describe common errors:</returns>
        //[DllImport("irprops.cpl", SetLastError = true)]
        //static extern IntPtr BluetoothFindFirstRadio(ref BLUETOOTH_FIND_RADIO_PARAM pbtfrp, out IntPtr phRadio);

        //[StructLayout(LayoutKind.Sequential)]
        //private struct LE_SCAN_REQUEST
        //{
        //    internal int scanType;
        //    internal ushort scanInterval;
        //    internal ushort scanWindow;
        //}

        //[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        //static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
        //ref LE_SCAN_REQUEST lpInBuffer, uint nInBufferSize,
        //IntPtr lpOutBuffer, uint nOutBufferSize,
        //out uint lpBytesReturned, IntPtr lpOverlapped);


        //public static void StartScanner(int scanType, ushort scanInterval, ushort scanWindow)
        //{

        //    //Action<object> action = (object obj) => {
        //    //    BLUETOOTH_FIND_RADIO_PARAM param = new BLUETOOTH_FIND_RADIO_PARAM();
        //    //    param.Initialize();
        //    //    IntPtr handle;
        //    //    BluetoothFindFirstRadio(ref param, out handle);
        //    //    uint outsize;
        //    //    LE_SCAN_REQUEST req = new LE_SCAN_REQUEST
        //    //    {
        //    //        scanType = scanType,
        //    //        scanInterval = scanInterval,
        //    //        scanWindow = scanWindow
        //    //    };
        //    //    DeviceIoControl(handle, 0x41118c, ref req, 8, IntPtr.Zero, 0, out outsize, IntPtr.Zero);
        //    //};
        //    //Task task = new Task(action, "nothing");
        //    //task.Start();
        //}


        public void StartBeaconManager()
        {
            //StartScanner(0, 29, 29);
            var provider = new WindowsBluetoothPacketProvider();
            _beaconManager = new BeaconManager(provider);
            _beaconManager.BeaconAdded += StartBeaconMonitoringAsync;
            _beaconManager.Start();
            if (provider.WatcherStatus == BLEAdvertisementWatcherStatusCodes.Started)
            {
                Analytics.TrackEvent("StartBeaconManager", new Dictionary<string, string> {
                           { "Category", "Beacon:" + provider.WatcherStatus  + ", " + BLEAdvertisementWatcherStatusCodes.Started},
                           { "Function", "StartBeaconManager"}
                        });
                //System.Diagnostics.Debug.WriteLine("WatchingForBeacons");
            }
        }

        public void StopWatchingBeaconManager()
        {
            System.Diagnostics.Debug.WriteLine("StoppedWatchingBeacons");
            _beaconManager.BeaconAdded -= StartBeaconMonitoringAsync;
            _beaconManager.Stop();
            _restartingBeaconWatch = false;

            Analytics.TrackEvent("StopWatchingBeaconManager", new Dictionary<string, string> {
                           { "Category", "Beacon:" + _restartingBeaconWatch  + ", " + BLEAdvertisementWatcherStatusCodes.Started},
                                   { "Function", "StopWatchingBeaconManager"}
                        });
        }

      

        private async void StartBeaconMonitoringAsync(object sender, Beacon beacon)
        {
           
            Ts trainMetaData = new Ts();
            trainMetaData.Date = beacon.Timestamp;

            var trainData = new TrainSpotterMessage();
            try
            {
                trainData.Rssi = beacon.Rssi;
                trainData.Mac = beacon.BluetoothAddressAsString;
                trainData.TrainBeacon = beacon.BluetoothAddressAsString;
                trainData.LastUpdate = beacon.Timestamp;
                Analytics.TrackEvent("StartBeaconMonitoringAsync", new Dictionary<string, string> {
                   { "Category", "Beacon:" + trainData.TrainBeacon},
                   { "Function", "StartBeaconMonitoringAsync"}
                });
                
                foreach (var bluetoothBeacon in _beaconManager.BluetoothBeacons.ToList())
                {                   
                    foreach (var beaconFrame in bluetoothBeacon.BeaconFrames.ToList())     //beacon.BeaconFrames.ToList())
                    {
                        if (beaconFrame is UidEddystoneFrame)     //https://github.com/google/eddystone/tree/master/eddystone-uid
                        {
                            trainData.BeaconType = "UidEddystoneFrame";
                            trainData.Name = "Eddystone UID Frame";
                            trainData.Distance = ((UidEddystoneFrame)beaconFrame).RangingData;     //Calibrated Tx power at 0 m   - value is an 8-bit integer and ranges from -100 dBm to +20 dBm
                                                                                                   // Note to developers: the best way to determine the precise value to put into this field is to measure the actual output of your beacon from 1 meter away and then add 41 dBm to that. 41dBm is the signal loss that occurs over 1 meter.
                            trainData.Namespace = ((UidEddystoneFrame)beaconFrame).NamespaceIdAsNumber.ToString("X") + " / " + ((UidEddystoneFrame)beaconFrame).InstanceIdAsNumber.ToString("X");
                        }
                        else if (beaconFrame is UrlEddystoneFrame)      //https://github.com/google/eddystone/tree/master/eddystone-url
                        {
                            trainData.BeaconType = "UrlEddystoneFrame";
                            trainData.Name = "Eddystone URL Frame";
                            trainData.Url = ((UrlEddystoneFrame)beaconFrame).CompleteUrl;
                            trainData.Distance = ((UrlEddystoneFrame)beaconFrame).RangingData;                        
                        }
                        else if (beaconFrame is TlmEddystoneFrame)      //https://github.com/google/eddystone/tree/master/eddystone-tlm
                        {
                            trainData.BeaconType = "TlmEddystoneFrame";
                            trainData.Name = "Eddystone Telemetry Frame";
                            trainData.Temperature = ((TlmEddystoneFrame)beaconFrame).TemperatureInC;
                            trainData.Battery = ((TlmEddystoneFrame)beaconFrame).BatteryInMilliV;
                            trainData.Uptime = ((TlmEddystoneFrame)beaconFrame).TimeSincePowerUp;
                            trainData.PacketsSent = ((TlmEddystoneFrame)beaconFrame).AdvertisementFrameCount;
                        }
                        else if (beaconFrame is EidEddystoneFrame)     //https://github.com/google/eddystone/tree/master/eddystone-eid
                        {
                            trainData.BeaconType = "EidEddystoneFrame";
                            trainData.Name = "Eddystone EID Frame";
                            trainData.Distance = ((EidEddystoneFrame)beaconFrame).RangingData;
                            //trainData.Eid   = BitConverter.ToString(((EidEddystoneFrame)beaconFrame).EphemeralIdentifier);
                        }                       
                        await AzureIoTHub.SendDeviceToCloudMessageAsync(trainData);      //send the data only for recognized data types
                        Analytics.TrackEvent("Sent Data", new Dictionary<string, string> {
                           { "Category", "Beacon:" + trainData.LastUpdate + ", " + trainData.Name + ", " + trainData.Url},
                           { "Function", "StartBeaconMonitoringAsync"}
                        });
                        await Task.Delay(1000);
                    }                
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in Parsing Data: " + ex.Message);
                await AzureIoTHub.SendDeviceToCloudMessageAsync(trainData);       //send what we can...
                Analytics.TrackEvent("Error Sent Data", new Dictionary<string, string> {
                           { "Category", "Error:" + ex.Message.ToString()},
                           { "Function", "StartBeaconMonitoringAsync"}
                        });
            }
            //StopWatchingBeaconManager();   //causes it to crash
            //await Task.Delay(1000);
            //StartBeaconManager();
        }

        #region Bluetooth Beacons
        /// <summary>
        /// Method demonstrating how to handle individual new beacons found by the manager.
        /// This event will only be invoked once, the very first time a beacon is discovered.
        /// For more fine-grained status updates, subscribe to changes of the ObservableCollection in
        /// BeaconManager.BluetoothBeacons (_beaconManager).
        /// To handle all individual received Bluetooth packets in your main app and outside of the
        /// library, subscribe to AdvertisementPacketReceived event of the IBluetoothPacketProvider
        /// (_provider).
        /// </summary>
        /// <param name="sender">Reference to the sender instance of the event.</param>
        /// <param name="beacon">Beacon class instance containing all known and parsed information about
        /// the Bluetooth beacon.</param>
        private void BeaconManagerOnBeaconAdded(object sender, Beacon beacon)
        {
            Debug.WriteLine("\nBeacon: " + beacon.BluetoothAddressAsString);
            Debug.WriteLine("Type: " + beacon.BeaconType);
            Debug.WriteLine("Last Update: " + beacon.Timestamp);
            Debug.WriteLine("RSSI: " + beacon.Rssi);

            Analytics.TrackEvent("Added Beacon", new Dictionary<string, string> {
                           { "Beacon", "Error:" + beacon.BluetoothAddressAsString},
                           { "Function", "BeaconManagerOnBeaconAdded"}
                        });

            foreach (var beaconFrame in beacon.BeaconFrames.ToList())
            {
                // Print a small sample of the available data parsed by the library
                if (beaconFrame is UidEddystoneFrame)
                {
                    Debug.WriteLine("Eddystone UID Frame");
                    Debug.WriteLine("ID: " + ((UidEddystoneFrame)beaconFrame).NamespaceIdAsNumber.ToString("X") + " / " +
                                    ((UidEddystoneFrame)beaconFrame).InstanceIdAsNumber.ToString("X"));
                }
                else if (beaconFrame is UrlEddystoneFrame)
                {
                    Debug.WriteLine("Eddystone URL Frame");
                    Debug.WriteLine("URL: " + ((UrlEddystoneFrame)beaconFrame).CompleteUrl);
                }
                else if (beaconFrame is TlmEddystoneFrame)
                {
                    Debug.WriteLine("Eddystone Telemetry Frame");
                    Debug.WriteLine("Temperature [°C]: " + ((TlmEddystoneFrame)beaconFrame).TemperatureInC);
                    Debug.WriteLine("Battery [mV]: " + ((TlmEddystoneFrame)beaconFrame).BatteryInMilliV);
                }
                else if (beaconFrame is EidEddystoneFrame)
                {
                    Debug.WriteLine("Eddystone EID Frame");
                    Debug.WriteLine("Ranging Data: " + ((EidEddystoneFrame)beaconFrame).RangingData);
                    Debug.WriteLine("Ephemeral Identifier: " + BitConverter.ToString(((EidEddystoneFrame)beaconFrame).EphemeralIdentifier));
                }
                else if (beaconFrame is ProximityBeaconFrame)
                {
                    Debug.WriteLine("Proximity Beacon Frame (iBeacon compatible)");
                    Debug.WriteLine("Uuid: " + ((ProximityBeaconFrame)beaconFrame).UuidAsString);
                    Debug.WriteLine("Major: " + ((ProximityBeaconFrame)beaconFrame).MajorAsString);
                    Debug.WriteLine("Major: " + ((ProximityBeaconFrame)beaconFrame).MinorAsString);
                }
                else
                {
                    Debug.WriteLine("Unknown frame - not parsed by the library, write your own derived beacon frame type!");
                    Debug.WriteLine("Payload: " + BitConverter.ToString(((UnknownBeaconFrame)beaconFrame).Payload));
                }
            }
        }

        private void WatcherOnStopped(object sender, BTError btError)
        {
           
            string errorMsg = null;
            if (btError != null)
            {
                switch (btError.BluetoothErrorCode)
                {
                    case BTError.BluetoothError.Success:
                        errorMsg = "WatchingSuccessfullyStopped";
                        break;
                    case BTError.BluetoothError.RadioNotAvailable:
                        errorMsg = "ErrorNoRadioAvailable";
                        break;
                    case BTError.BluetoothError.ResourceInUse:
                        errorMsg = "ErrorResourceInUse";
                        break;
                    case BTError.BluetoothError.DeviceNotConnected:
                        errorMsg = "ErrorDeviceNotConnected";
                        break;
                    case BTError.BluetoothError.DisabledByPolicy:
                        errorMsg = "ErrorDisabledByPolicy";
                        break;
                    case BTError.BluetoothError.NotSupported:
                        errorMsg = "ErrorNotSupported";
                        break;
                    case BTError.BluetoothError.OtherError:
                        errorMsg = "ErrorOtherError";
                        break;
                    case BTError.BluetoothError.DisabledByUser:
                        errorMsg = "ErrorDisabledByUser";
                        break;
                    case BTError.BluetoothError.ConsentRequired:
                        errorMsg = "ErrorConsentRequired";
                        break;
                    case BTError.BluetoothError.TransportNotSupported:
                        errorMsg = "ErrorTransportNotSupported";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            if (errorMsg == null)
            {
                // All other errors - generic error message
                errorMsg = _restartingBeaconWatch
                    ? "FailedRestartingBluetoothWatch"
                    : "AbortedWatchingBeacons";
            }
            Analytics.TrackEvent("Watcher Stopped", new Dictionary<string, string> {
                           { "Beacon", "Error:"  + errorMsg},
                           { "Function", "WatcherOnStopped"}
                        });
            //SetStatusOutput(_resourceLoader.GetString(errorMsg));
        }

#if DEBUG
        private void PrintBeaconInfoExample()
        {
            Debug.WriteLine("Beacons discovered so far\n-------------------------");
            foreach (var bluetoothBeacon in _beaconManager.BluetoothBeacons.ToList())
            {
                Debug.WriteLine("\nBeacon: " + bluetoothBeacon.BluetoothAddressAsString);
                Debug.WriteLine("Type: " + bluetoothBeacon.BeaconType);
                Debug.WriteLine("Last Update: " + bluetoothBeacon.Timestamp);
                Debug.WriteLine("RSSI: " + bluetoothBeacon.Rssi);
                foreach (var beaconFrame in bluetoothBeacon.BeaconFrames.ToList())
                {
                    // Print a small sample of the available data parsed by the library
                    if (beaconFrame is UidEddystoneFrame)
                    {
                        Debug.WriteLine("Eddystone UID Frame");
                        Debug.WriteLine("ID: " + ((UidEddystoneFrame)beaconFrame).NamespaceIdAsNumber.ToString("X") + " / " +
                                        ((UidEddystoneFrame)beaconFrame).InstanceIdAsNumber.ToString("X"));
                    }
                    else if (beaconFrame is UrlEddystoneFrame)
                    {
                        Debug.WriteLine("Eddystone URL Frame");
                        Debug.WriteLine("URL: " + ((UrlEddystoneFrame)beaconFrame).CompleteUrl);
                    }
                    else if (beaconFrame is TlmEddystoneFrame)
                    {
                        Debug.WriteLine("Eddystone Telemetry Frame");
                        Debug.WriteLine("Temperature [°C]: " + ((TlmEddystoneFrame)beaconFrame).TemperatureInC);
                        Debug.WriteLine("Battery [mV]: " + ((TlmEddystoneFrame)beaconFrame).BatteryInMilliV);
                    }
                    else if (beaconFrame is EidEddystoneFrame)
                    {
                        Debug.WriteLine("Eddystone EID Frame");
                        Debug.WriteLine("Ranging Data: " + ((EidEddystoneFrame)beaconFrame).RangingData);
                        Debug.WriteLine("Ephemeral Identifier: " + BitConverter.ToString(((EidEddystoneFrame)beaconFrame).EphemeralIdentifier));
                    }
                    else if (beaconFrame is ProximityBeaconFrame)
                    {
                        Debug.WriteLine("Proximity Beacon Frame (iBeacon compatible)");
                        Debug.WriteLine("Uuid: " + ((ProximityBeaconFrame)beaconFrame).UuidAsString);
                        Debug.WriteLine("Major: " + ((ProximityBeaconFrame)beaconFrame).MajorAsString);
                        Debug.WriteLine("Major: " + ((ProximityBeaconFrame)beaconFrame).MinorAsString);
                    }
                    else
                    {
                        Debug.WriteLine("Unknown frame - not parsed by the library, write your own derived beacon frame type!");
                        Debug.WriteLine("Payload: " + BitConverter.ToString(((UnknownBeaconFrame)beaconFrame).Payload));
                    }
                }
            }
        }
#endif

        #endregion

      
        #region Tools
        /// <summary>
        /// Convert minus-separated hex string to a byte array. Format example: "4E-66-63"
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static byte[] HexStringToByteArray(string hex)
        {
            // Remove all space characters
            var hexPure = hex.Replace("-", "");
            if (hexPure.Length % 2 != 0)
            {
                // No even length of the string
                throw new Exception("No valid hex string");
            }
            var numberChars = hexPure.Length / 2;
            var bytes = new byte[numberChars];
            var sr = new StringReader(hexPure);
            try
            {
                for (var i = 0; i < numberChars; i++)
                {
                    bytes[i] = Convert.ToByte(new string(new[] { (char)sr.Read(), (char)sr.Read() }), 16);
                }
            }
            catch (Exception)
            {
                throw new Exception("No valid hex string");
            }
            finally
            {
                sr.Dispose();
            }
            return bytes;
        }
        #endregion
    }
}
