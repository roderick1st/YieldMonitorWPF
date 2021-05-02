using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YieldMonitorWPF
{
    class BluetoothDeviceControl
    {
        //Event Handling follow the numbers to make sense
        public event BTDeviceHandler BTDeviceHandlerEvent; //SCAN event handler

//SCANNING__________________________________________________________________________________________________________
        public void Scan()
        {
            BluetoothClient btClient = new BluetoothClient();
            BluetoothDeviceInfo[] btDevices = btClient.DiscoverDevices().ToArray();
            foreach (BluetoothDeviceInfo d in btDevices)
            {
                //have we found the device we are looking for?
                if (d.DeviceName == "DSD TECH HC-05")
                {
                    SendBluetoothEvent(1, d.DeviceName, d.DeviceAddress, false);//Scan notification = 1
                    Pair(d.DeviceAddress);
                    break;
                }
            }
        }

//PAIRING___________________________________________________________________________________________________________
        public void Pair(ulong myDeviceAddress)
        {
            bool devicePairSuccess = false;
            
            //is the device already paired?
            BluetoothClient bluetoothClient = new BluetoothClient();
            var pairedDevices = bluetoothClient.PairedDevices;

            foreach (BluetoothDeviceInfo device in pairedDevices)
            {
                if (device.DeviceAddress == myDeviceAddress)//Dont try to Pair as paired already
                {
                    Debug.WriteLine("Device already paired");
                    devicePairSuccess = true;
                    SendBluetoothEvent(2, "", 0, devicePairSuccess);//Paired notification = 2
                }
            }

            //If device isnt paired lets pair it now
            if(devicePairSuccess == false)
            {
                devicePairSuccess = PerformPair(myDeviceAddress);
                Debug.WriteLine("Device Paired = " + devicePairSuccess);
                SendBluetoothEvent(2, "", 0, devicePairSuccess);//Paired notification = 2
            }
        }

        public bool PerformPair (ulong myDeviceAddress)
        {
            Debug.WriteLine("Attempting Pair");
            bool devicePairSuccess;
            try
            {
                devicePairSuccess = BluetoothSecurity.PairRequest(myDeviceAddress, "1234");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SendBluetoothEvent (int myEventType, string myDeviceName, ulong myDeviceAddress, bool isDevicePaired)
        {
            SendBTDeviceEventArgs sendBTDevicePairArgs = new SendBTDeviceEventArgs() { eventType = myEventType, deviceName = myDeviceName, deviceAddress = myDeviceAddress,  devicePaired = isDevicePaired };
            BTDeviceHandlerEvent.Invoke(null, sendBTDevicePairArgs);//send the event
        }
    }


    //USED FOR BT Events
    public class SendBTDeviceEventArgs : EventArgs //Event Type, Device Name, Device Address, Paired Status
    {
        public int eventType { get; set; }
        public string deviceName { get; set; }
        public ulong deviceAddress { get; set; }
        public bool devicePaired { get; set; }
    }
    public delegate void BTDeviceHandler(object sender, SendBTDeviceEventArgs args); // [7] Delegate used to send across threads

}
