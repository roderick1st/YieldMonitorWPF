using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YieldMonitorWPF
{
    class UDPPortConnection
    {
        //Event Handling follow the numbers to make sense
        public event UDPPortdHandler UDPPortEvent; //[6] DateTimeRecievedHandler is the delegate

        public void ReadFromUDPPort(int myPort)
        {       
            try
            {
                UdpClient receivingUdpClient = new UdpClient();
                IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, myPort);
                receivingUdpClient.ExclusiveAddressUse = false;
                receivingUdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                receivingUdpClient.Client.Bind(remoteIpEndPoint);
                while (true)
                {
                    Byte[] receiveBytes = receivingUdpClient.Receive(ref remoteIpEndPoint);
                    string returnData = Encoding.ASCII.GetString(receiveBytes);
                    if(returnData.Length > 0)
                    {
                        SendUDPPortArgs args = new SendUDPPortArgs() { gpsData = returnData, ThreadEnd = true };
                        UDPPortEvent.Invoke(null, args);
                    }
                    
                }
            }
            catch
            {
                SendUDPPortArgs args = new SendUDPPortArgs() { ThreadEnd = true }; //thread has closed because of error
                UDPPortEvent.Invoke(null, args);
            }

        }

    }


    public class SendUDPPortArgs : EventArgs //[4] Our data layout
    {
        //public string[] portList { get; set; }
        public string gpsData { get; set; }
        public bool ThreadEnd { get; set; }
    }
    public delegate void UDPPortdHandler(object sender, SendUDPPortArgs args); // [7] Delegate used to send across threads
}
