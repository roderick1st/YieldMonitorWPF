using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YieldMonitorWPF
{
    class GetTimeAndDate
    {

        //Event Handling follow the numbers to make sense
        public event DateTimeRecievedHandler DateTimeRecievedEvent; //[6] DateTimeRecievedHandler is the delegate

        public void Run()
        {
            Debug.WriteLine("In DateAndTime Class");
            string sDateTime;
            string sDate;
            string sTime;

            try
            {
                while (true)
                {
                    DateTime dateTime = DateTime.Now;
                    sDateTime = dateTime.ToString();
                    sDate = sDateTime.Substring(0, 10);
                    sTime = sDateTime.Substring(11, sDateTime.Length - 11);
                    OnDateTimeRecieved(EventArgs.Empty, sDate, sTime, false);// [1] send the date time to Onrecieved function [2]
                    Thread.Sleep(1000);
                }
            }
            catch
            {
                //task ending event will be sent
            }           
            //Thread about to end
            OnDateTimeRecieved(EventArgs.Empty, "", "", true);
        }
        protected virtual void OnDateTimeRecieved(EventArgs e, string myNewDate, string myNewTime, bool taskEnding) // [2] Recieves the new date and time
        {
            SendDateTimeDataArgs args = new SendDateTimeDataArgs() { newDate = myNewDate, newTime = myNewTime }; //[3] Sets the data out as outlined in 4
            DateTimeRecievedEvent.Invoke(null, args); // [5] Pass the data to the event
        }
    }


    //Configure our own arguments to send back as data
    public class SendDateTimeDataArgs : EventArgs //[4] Our data layout
    {
        public string newDate { get; set; }
        public string newTime { get; set; }
        public bool ThreadEnd { get; set; }
    }
    public delegate void DateTimeRecievedHandler(object sender, SendDateTimeDataArgs args); // [7] Delegate used to send across threads
 
}
