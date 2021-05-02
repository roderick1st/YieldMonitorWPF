using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace YieldMonitorWPF

{
    class SerialPortConnection
    {

        //POAGI structure
        //"$PAGOGI,Fix Taken, Latitude, N or S, Longitude, E or W, Fix Quality, Number of Sat, HDOP, M above Sea,
        string[] myPAOGI = { "$PAOGI","","","","","","","","","","","","",""}; //global string array to hold the POAGI
        bool myPAOGIBuilt = false; //only send out the POAGI if its built

        //Event Handling follow the numbers to make sense
        public event SerialPortdHandler SerialPortEvent; //[6] DateTimeRecievedHandler is the delegate

        public void GetSerialPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            if (ports.Length > 0) //we have ports send them back to UI to populate combo box
            {
                OnPortsRecieved(EventArgs.Empty, ports); //send port list out
            }
        }

        protected virtual void OnPortsRecieved(EventArgs e, string[] myPortList) // [2] Recieves the new date and time
        {
            SendSerialPortArgs args = new SendSerialPortArgs() { portList = myPortList}; //[3] Sets the data out as outlined in 4
            SerialPortEvent.Invoke(null, args); // [5] Pass the data to the event
        }

        public void ReadFromSerialPort(string selectedSerialPort)
        {
            string myGPSMessage;
            SerialPort serialPort = new SerialPort();
            serialPort.PortName = selectedSerialPort;

            try
            {
                serialPort.Open();
                while (true)
                {
                    myGPSMessage = serialPort.ReadLine();
                    if ((myGPSMessage.Length != null)|(myGPSMessage.Length > 0))
                    {
                        if(myGPSMessage.Substring(0,1) == "$")//if it starts with $ its probably GPS
                        {
                            Debug.Write(myGPSMessage);
                            
                            OnDataRecieved(EventArgs.Empty, myGPSMessage, true);
                            //SendOutUDPData(myGPSMessage); //send the UDP data out
                        }
                    }                                       
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR : " + e.Message);
            }

            //thread comming to end .. need to warn as shouldnt
            OnDataRecieved(EventArgs.Empty,"", false);//warn main program
        }
        protected virtual void OnDataRecieved(EventArgs e, string myGPSData, bool myThreadEnd)
        {
            SendSerialPortArgs args = new SendSerialPortArgs() { gpsData = myGPSData, ThreadEnd = myThreadEnd }; 
            SerialPortEvent.Invoke(null, args); 
        }


        private void SendOutUDPData(string myGPSMessage)
        {
            //We need to build an OGI strin            
            //BuildPOAGI(myGPSMessage);

            string myPAOGIString = BuildPOAGI(myGPSMessage);
            //Debug.Write(myPAOGIString);

            if(myPAOGIString != "")
            {
                IPEndPoint localpt = new IPEndPoint(IPAddress.Parse("192.168.2.255"), 9999);
                UdpClient udpClient = new UdpClient();

                Byte[] sendMessage = Encoding.ASCII.GetBytes(myPAOGIString);
                //needs to be in try I tyhink
                try
                {
                    udpClient.Send(sendMessage, sendMessage.Length,localpt);
                    udpClient.Close();
                }
                catch
                {

                }                
            }    
        }

        private string BuildPOAGI(string myGPSMessage)
        {

            string myPOAGI = ""; //global string to hold the POAGI
            //what is the message
            switch (myGPSMessage.Substring(3, 3))
            {
                case "GGA":
                    //break the string into a list
                    string[] gpsGGAMessage = myGPSMessage.Split(",");// split the message by ,
                    myPOAGI = myPOAGI + "$PAOGI,"; //first item
                    myPOAGI = myPOAGI + gpsGGAMessage[1] + ",";//Fix taken
                    myPOAGI = myPOAGI + gpsGGAMessage[2] + ",";// latitude
                    myPOAGI = myPOAGI + gpsGGAMessage[3] + ",";// N or S
                    myPOAGI = myPOAGI + gpsGGAMessage[4] + ",";// longitude
                    myPOAGI = myPOAGI + gpsGGAMessage[5] + ","; // E or W
                    myPOAGI = myPOAGI + gpsGGAMessage[6] + ","; //fix quality
                    myPOAGI = myPOAGI + gpsGGAMessage[7] + ","; //number of satellites tracked
                    myPOAGI = myPOAGI + gpsGGAMessage[8] + ","; //HDOP Horizontal dilution of position
                    myPOAGI = myPOAGI + gpsGGAMessage[9] + ","; //meters above sea level
                    myPOAGI = myPOAGI + ",,,,,,";
                    //calculate checksum of the message
                    var byteChecksum = 0;
                    //int iChecksum;
                    string sChecksum;

                    //convert the string to a byte array
                    byte[] GPSByteArray = Encoding.ASCII.GetBytes(myPOAGI);

                    //go through the array ignoring $ and , to create checksum
                    for (int i = 1; i < GPSByteArray.Length; i++)
                    {
                        if ((GPSByteArray[i] != 0x24) & (GPSByteArray[i] != 0x2c)) //0x24 = $     0x2c = ,
                        {
                            //Debug.Write(Convert.ToChar(GPSByteArray[i]));
                            byteChecksum ^= GPSByteArray[i];
                        }                        
                    }
                    //iChecksum = byteChecksum;
                    sChecksum = byteChecksum.ToString("X2");
                    myPOAGI = myPOAGI + "*" + sChecksum + "\r\n"; //add checksum and line return charaters.
                    
                    break;

                case "GSV":
                    break;
            }

            return myPOAGI;
        }
    }


    public class SendSerialPortArgs : EventArgs //[4] Our data layout
    {
        public string[] portList { get; set; }
        public string gpsData { get; set; }
        public bool ThreadEnd { get; set; }
    }
    public delegate void SerialPortdHandler(object sender, SendSerialPortArgs args); // [7] Delegate used to send across threads

}
