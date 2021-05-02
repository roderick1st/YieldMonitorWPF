using InTheHand.Net.Sockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YieldMonitorWPF
{
    class BluetoothReadData
    {
        Guid myGuid = Guid.Parse("00001101-0000-1000-8000-00805F9B34FB");

        public event BluetoothDataRecievedHandler BluetoothDataRecievedEvent;

        public void Run(ulong deviceAddress)
        {
            // Connect to device
            BluetoothClient bluetoothClient = new BluetoothClient();
            bluetoothClient.Connect(deviceAddress, myGuid);
            Stream myStream = bluetoothClient.GetStream();//get the stream from the device

            byte[] mBuffer = new byte[1024];
            int numBytes; //bytes returned
            string sDeviceData;
            string sIncompletReturnedData = "";
            string sAllData;

            while (true)
            {
                //try
                //{
                    numBytes = myStream.Read(mBuffer, 0, 1024);
                    if (numBytes > 0)
                    {
                        sDeviceData = Encoding.ASCII.GetString(mBuffer, 0, numBytes);
                        sAllData = sIncompletReturnedData + sDeviceData;
                        //Debug.WriteLine("Returned Data: " + sIncompletReturnedData);
                        sIncompletReturnedData = "";
                        sDeviceData = "";

                        //lets get this data sorted out
                        //pass it to a new procedure to keep things tidy the proc will return any incomplete string to be added to the next stream
                        sIncompletReturnedData = SortTheStreamOut(sAllData);
                        sAllData = "";
                        Thread.Sleep(100); //more time to collect data?
                    }

                //}
                //catch
                //{

                //}
            }

        }

        private string SortTheStreamOut(string sStreamData)
        {
            int iStreamDataLength = sStreamData.Length;
            string sLeftOverStream = "";
            char sFirstChar;
            char sLastChar;
            int yIndex;
            int PIndex;

            if (iStreamDataLength > 0)//test if we have any data
            {
                string[] dataLines = sStreamData.Split(new[] { "\r\n" }, StringSplitOptions.None);
                //Debug.WriteLine("dataLines size : " + dataLines.Length);
                foreach (string line in dataLines)
                {
                    //Debug.WriteLine(line);
                    if (line.Length > 0) // make sure we have soome data
                    {
                        //check its a full line
                        //needs to start with a 'Y' and end with a 'p'
                        sFirstChar = line[0];
                        sLastChar = line[line.Length - 1];

                        if ((sFirstChar == 'Y') && (sLastChar == 'p'))//we have a full line
                        {
                            //find the 'y' and the 'P'
                            yIndex = line.IndexOf('y');
                            PIndex = line.IndexOf('P');

                            //Convert to long and sent to handler
                            SendTimeDataOutEvent(long.Parse(line.Substring(1, yIndex - 1)), long.Parse(line.Substring(PIndex + 1, line.Length - PIndex - 2)));
                        }
                        else if ((sFirstChar == 'Y') && (sLastChar != 'p'))//We have a begining but no end so add it to the next stream read
                        {
                            sLeftOverStream = line;
                        }
                    }
                }
            }
            return sLeftOverStream;
        }

        private void SendTimeDataOutEvent(long lYieldData, long lPaddleData)
        {
            if (lYieldData > 0) // stop erronious blanks from getting posted
            {
                OnBluetoothDataRecieved(EventArgs.Empty, lYieldData, lPaddleData);
            }
        }

        protected virtual void OnBluetoothDataRecieved(EventArgs e, long lYield, long lPaddle)
        {
            if (BluetoothDataRecievedEvent != null)
            {
                SendBluetoothDataArgs args = new SendBluetoothDataArgs() { yieldTime = lYield, paddleTime = lPaddle };
                BluetoothDataRecievedEvent.Invoke(null, args);
            }
            else
            {
                //No subscribers
            }
        }
    }
}


    //Configure our own arguments to send back as data
    public class SendBluetoothDataArgs : EventArgs //[4] Our data layout
    {
        public long yieldTime { get; set; }
        public long paddleTime { get; set; }
    }
    public delegate void BluetoothDataRecievedHandler(object sender, SendBluetoothDataArgs args); // [7] Delegate used to send across threads

