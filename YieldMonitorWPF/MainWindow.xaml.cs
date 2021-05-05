using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;

namespace YieldMonitorWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //Global Variable
        ulong glob_DeviceAddress = 0;
        //bool glob_DevicePaired = false;
        Task glob_serialPortTask = null;
        Task glob_timeDateTask = null;
        Task glob_BluetoothReadTask = null;
        Task glob_BluetoothScanTask = null;
        Task glob_SaveDataToFile = null;

        int glob_GPSPort = 7999; //port used to recieve GPS from F9P

        bool glob_RecordField = false;
        bool glob_StopRecording = false;

        //static string dataPath = "E:/c_Sharp_Solutions/YieldMonitorWPF/YieldMonitorWPF/";
        static string dataPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\YieldMonitor\\";
        static string xmlFieldFile = dataPath + "Fields.xml";


        //string glob_AccumalatedDataList = new List<DataToCollect>(); //use a class called DataToCollect to store the data we want to send to our file to save.
        static List<DataToCollect> dataToCollectList;// stores batchs of data for averaging
        static List<DataToCollect> dataToCollectForFile; //stores data to be saved in a file

        GPSData currentGPSData = new GPSData();//place to store GPS data

        public MainWindow()
        {
            InitializeComponent(); 
            this.Closed += Window_Closed;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CheckFileStructure();//check that the file structure is in place and build it if not
            MonitorTasks();//start timer to monitor task status
            ListSerialPorts(); //pull in comports .. start reading only if com port selected.
            buttonStart.IsEnabled = false;// dont lets us click untill we have a device
            buttonStart.Content = "Connect";
            StartCollectingDateAndTime(); //Lets collect the time  
            ScanForBluetooth(); // this will wait to complete...
            GetFieldsFromXML(-2); //populate the fields combo box
            
            comboboxComPort.SelectedIndex = 1;
        }

        private void CheckFileStructure()
        {
            //Does the folder YieldMonitor exist in the users MyDocuments folder
            if (!Directory.Exists(dataPath))
            {
                //create the directory
                Directory.CreateDirectory(dataPath);
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            //save current state to xml
            ProgramClosed programClosed = new ProgramClosed();
            //save field name, combobox index and gps combobox index
            programClosed.SaveSettings(dataPath, comboboxFields.SelectedItem.ToString(), comboboxFields.SelectedIndex, comboboxComPort.SelectedIndex);
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {

            if (buttonStart.Content.ToString() == "Start Recording")//we have a working connection
            {
                buttonStart.Content = "Recording...";
                glob_RecordField = true;//start saving the field data to a file
            }
            else if (buttonStart.Content.ToString() == "Recording...")
            {
                buttonStart.Content = "Start Recording";
                glob_RecordField = false;
                glob_StopRecording = true; //we need to save our collected data befor stopping the recordning
            }              
        }

        private void ConnectToBlueTooth()
        {
            //dont like this ATM but we need to check we are paired and have the device available
            if (glob_DeviceAddress != 0) //do we know of an address?
            {
                BluetoothDeviceControl bluetoothDeviceControl = new BluetoothDeviceControl();
                bluetoothDeviceControl.BTDeviceHandlerEvent += DeviceStillConnected;
                Task.Factory.StartNew(() => bluetoothDeviceControl.Pair(glob_DeviceAddress), TaskCreationOptions.LongRunning);
            }
            else // look for the device
            {
                ScanForBluetooth();
            }
        }

        private void buttonAddField_Click(object sender, RoutedEventArgs e)
        {
            AddRemoveField addRemoveField = new AddRemoveField(); //add field class
            addRemoveField.addField(xmlFieldFile, textboxAddField.Text, textboxLatitude.Text, textboxLongitude.Text, textboxDate.Text, textboxTime.Text);
            GetFieldsFromXML(-1); //reload the combobox with new xml data            
        }

        private void comboboxFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {          
        }

        private void GetFieldsFromXML(int itemSelected)
        {
            //clear the combobox
            comboboxFields.Items.Clear();

            //Build the dataset from the xml file
            AddRemoveField addRemoveField = new AddRemoveField();
            foreach(DataRow row in addRemoveField.buildDataSet(xmlFieldFile).Tables[0].Rows)
            {
                string comboField = row.ItemArray[0] + " - " + row.ItemArray[1] + " " + row.ItemArray[2];
                comboboxFields.Items.Add(comboField); //add the first column of data into the combo box
            }

            //decide which item to select in the combobox
            if(itemSelected == -1) //we want the last element loaded
            {                
                comboboxFields.SelectedIndex = comboboxFields.Items.Count - 1; //select the last item
            } else if (itemSelected == -2) //we want the last field used before the program was closed
            {
                try
                {
                    ProgramClosed programStarted = new ProgramClosed();
                    comboboxFields.SelectedItem = programStarted.LoadSettings(dataPath, "LastSettings.xml");
                } catch //incase the field does not exist anymore
                {
                    comboboxFields.SelectedIndex = 0;
                }
            }           
            else
            {
              comboboxFields.SelectedIndex = itemSelected; //load the first item
            }
            
        }

        private void comboboxComPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //a com port has been selected try to read from it
            string selectedComPort = comboboxComPort.SelectedItem.ToString();           

            if (selectedComPort.Substring(0,3) == "COM") //we have a com port
            {
                SerialPortConnection mySerialPortConnection = new SerialPortConnection();
                mySerialPortConnection.SerialPortEvent += SerialPortTaskActive; //coolect serial events and check if task is running                                                                                
                glob_serialPortTask = Task.Factory.StartNew(() => mySerialPortConnection.ReadFromSerialPort(selectedComPort), TaskCreationOptions.LongRunning);
                //var taskStatus = glob_serialPortTask.Status;
            }
            if (comboboxComPort.SelectedIndex == 1) //we have a UDP source
            {
                //comboboxComPort.SelectedItem = "UDP Port : " + glob_GPSPort.ToString(); //tell the user which port we are trying to connect to
                UDPPortConnection myUDPPortConnection = new UDPPortConnection();
                myUDPPortConnection.UDPPortEvent += UDPPortTaskActive;
                glob_serialPortTask = Task.Factory.StartNew(() => myUDPPortConnection.ReadFromUDPPort(glob_GPSPort), TaskCreationOptions.LongRunning);
            }
        }

        //GET SERIAL PORTS FOR GPS_____________________________________________________________________________________
        private void ListSerialPorts()//add ports to drop down list
        {
            comboboxComPort.Items.Clear();
            SerialPortConnection serialPortConnection = new SerialPortConnection();
            serialPortConnection.SerialPortEvent += FillSerialCombo;
            Task.Factory.StartNew(() => serialPortConnection.GetSerialPorts(), TaskCreationOptions.LongRunning);
        }
        private void FillSerialCombo(object sender, SendSerialPortArgs args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                comboboxComPort.Items.Add("Choose GPS port");
                comboboxComPort.Items.Add("UDP Source");
                foreach (string port in args.portList)
                {
                    comboboxComPort.Items.Add(port);
                }
            }), DispatcherPriority.Background);
        }
        //GET SERIAL PORTS FOR GPS END_________________________________________________________________________________

        //GPS DATA_________________________________________________________________________________________________________
  
        private float CalculateLatitudeLongitude(string myNMEAType, string currentLatitudeOrLongitude, string northSouthEastWest)
        {
            //(d)dd + (mm.mmmm / 60)(*-1 for W and S)
            int pointPosition = currentLatitudeOrLongitude.IndexOf(".");
            float latlongDegrees = float.Parse(currentLatitudeOrLongitude.Substring(0, pointPosition - 2));
            float latlongMinutes = float.Parse(currentLatitudeOrLongitude.Substring(pointPosition - 2, currentLatitudeOrLongitude.Length - pointPosition + 2));
            float fLatitudeLongitude = latlongDegrees + (latlongMinutes / 60);
            if ((northSouthEastWest == "S")|(northSouthEastWest == "W")) { fLatitudeLongitude = -1 * fLatitudeLongitude; }
            //weHaveLat = true;
            return fLatitudeLongitude;
        }

        //Deal with GPS data from UDP
        private void UDPPortTaskActive(object sender, SendUDPPortArgs args)
        {
            if (args.ThreadEnd != false)//Serial Port Task has not ended .. it shouldnt
            {
                if(args.gpsData != null)//dont process string if its null... will it ever be null?
                {
                    GPSDataProcessing gPSDataString = new GPSDataProcessing(); //create instance of GPS claas
                    gPSDataString.Process(args.gpsData); //pass data to class
                }
                

                NewGPSDataRecieved(args.gpsData);
            }
            else
            {
                UnexpectedThreadEnd(2);
            }
        }

        private void SerialPortTaskActive(object sender, SendSerialPortArgs args)
        {
            if (args.ThreadEnd != false)//Serial Port Task has not ended .. it shouldnt
            {
                NewGPSDataRecieved(args.gpsData);
            }
            else
            {
                UnexpectedThreadEnd(2);
            }
        }

        private void NewGPSDataRecieved(string gpsDataLine)// sort it out and send to UI
        {

            bool postLat = false; 
            bool postLon = false;
            bool postalt = false;
            bool postQual = false;
            bool postHDOP = false;
            bool postPDOP = false;
            bool postCM = false;
            bool postKph = false;
            bool postVDOP = false;


            GPSData tempGPSData = new GPSData(); //local store from class
            GPSDataProcessing newGPSData = new GPSDataProcessing();//send stringto class for processing and store the returned data
            tempGPSData = newGPSData.Process(gpsDataLine);

            if(tempGPSData.Lat != -1) { currentGPSData.Lat = tempGPSData.Lat; postLat = true; }//we have lat
            if(tempGPSData.Lon != -1) { currentGPSData.Lon = tempGPSData.Lon; postLon = true; }//we have lon
            if(tempGPSData.Alt != -1) { currentGPSData.Alt = tempGPSData.Alt; postalt = true; }//we have alt reading
            if(tempGPSData.GpsQuality != -1) { currentGPSData.GpsQuality = tempGPSData.GpsQuality; postQual = true; }//we have quality
            if(tempGPSData.HDOP != -1) { currentGPSData.HDOP = tempGPSData.HDOP; postHDOP = true; }//we have HDOP
            if(tempGPSData.PDOP != -1) { currentGPSData.PDOP = tempGPSData.PDOP;  postPDOP = true; }//we have PDOP
            if(tempGPSData.SpeedCMs != -1) { currentGPSData.SpeedCMs = tempGPSData.SpeedCMs;  postCM = true; }//we have speed in cm
            if(tempGPSData.SpeedKph != -1) { currentGPSData.SpeedKph = tempGPSData.SpeedKph;  postKph = true; }//we have speed in kph
            if(tempGPSData.VDOP != -1) { currentGPSData.VDOP = tempGPSData.VDOP;  postVDOP = true; }//we have VDOP
    
                Dispatcher.BeginInvoke(new Action(() =>
                    {
                    //deal with 0 data
                    if (postLat & postLon) //display lat and long
                        {
                            textboxLatitude.Text = tempGPSData.Lat.ToString();
                            textboxLongitude.Text = tempGPSData.Lon.ToString();
                        }

                        if (postalt)//post altitude
                        {
                            textboxAltitude.Text = tempGPSData.Alt.ToString();
                        }

                        if (postCM)//we will always have Kph if CM
                        {
                            if(tempGPSData.SpeedKph < 1) //if we are slower than 1kph
                            {
                                //post cm to screen
                                textBlockSpeed.Text = "Speed (cm/s)";
                                textboxSpeed.Text = tempGPSData.SpeedCMs.ToString();
                            } else
                            {
                                //post as KPH
                                textBlockSpeed.Text = "Speed (kph)";
                                textboxSpeed.Text = tempGPSData.SpeedKph.ToString();
                            }
                        }                                                    
                    }), DispatcherPriority.Background);
        }
        //GPS DATA END_____________________________________________________________________________________________________


        private void DeviceStillConnected(object sender, SendBTDeviceEventArgs args)
        {
            if (args.devicePaired == true)//if the device is paired
            {
                Debug.WriteLine("Device is Still Paired");
                //create a list to store data
                StreamBluetoothData();  //start reading from the device
            }
        }

//DATE TIME_______________________________________________________________________________________________________
        private void StartCollectingDateAndTime()
        {
            //start new task to get the time and date
            GetTimeAndDate myDateAndTime = new GetTimeAndDate();
            myDateAndTime.DateTimeRecievedEvent += NewDateTimeEventRecieved; //If we recieve a new date time from GetDateAndTime, send it to function NewDateTimeEventRecieved
            glob_timeDateTask = Task.Factory.StartNew(() => myDateAndTime.Run(), TaskCreationOptions.LongRunning);
            //Event will be collected and handled in OnDateTimeRecieved()
        }
        private void NewDateTimeEventRecieved(object sender, SendDateTimeDataArgs args)//Run when event from GetTimeAndDate runs
        {
            if (args.ThreadEnd)
            {
                UnexpectedThreadEnd(1);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    textboxDate.Text = args.newDate;
                    textboxTime.Text = args.newTime;
                }), DispatcherPriority.Background);   
            }
             
        }
//END DATE TIME___________________________________________________________________________________________________

//BLUETOOTH SCAN  ________________________________________________________________________________________________
        private async void ScanForBluetooth()
        {
            //Reset our BT connection stuff
            glob_DeviceAddress = 0;
            buttonStart.Content = "Scanning";
            buttonStart.IsEnabled = false;
            //Lets look for the device
            textboxBluetoothDevice.Text = "Scanning...";
            BluetoothDeviceControl myBluetoothDeviceControl = new BluetoothDeviceControl();
            myBluetoothDeviceControl.BTDeviceHandlerEvent += BluetoothDeviceControlEventController;
            //await Task.Factory.StartNew(() => myBluetoothDeviceControl.Scan(), TaskCreationOptions.LongRunning);
            glob_BluetoothScanTask = Task.Factory.StartNew(() => myBluetoothDeviceControl.Scan(), TaskCreationOptions.LongRunning);
            await glob_BluetoothScanTask;
            //if the task finnishes and we dont have a device try again
            if (glob_DeviceAddress == 0)//scan for bluetooth again
            {
                ScanForBluetooth(); //NOT TESTED
            }
            else
            {
                ConnectToBlueTooth(); // streaming from BT device
                buttonStart.Content = "Start Recording";
                buttonStart.IsEnabled = true;
            }           
        }
        private void BluetoothDeviceControlEventController(object sender, SendBTDeviceEventArgs args)
        {
            switch (args.eventType)
            {
                case 1:// scan event
                    NewBluetoothDeviceFound(args.deviceName, args.deviceAddress);
                    break;

                case 2://Pair Event
                    BluetoothDevicePaired(args.devicePaired);
                    break;
            }
        }
        private void NewBluetoothDeviceFound(string deviceName, ulong deviceAddress)
        {
            glob_DeviceAddress = deviceAddress;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                buttonStart.IsEnabled = true;
                buttonStart.Content = "Start Recording";
                textboxBluetoothDevice.Text = deviceName + " - " + deviceAddress.ToString();
            }), DispatcherPriority.Background);
            
        }
//end BLUETOOTH SCAN _____________________________________________________________________________________________
        
        private void BluetoothDevicePaired(bool devicePaired)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (devicePaired)
                {
                    textblockBluetoothDevice.Text = "Bluetooth Device - Paired";
                }
                else
                {
                    textblockBluetoothDevice.Text = "Bluetooth Device - Failed";
                }
            }), DispatcherPriority.Background);
        }
//end PAIR      __________________________________________________________________________________________________


//READING DATA FROM BLUETOOTH_____________________________________________________________________________________
        private void StreamBluetoothData() //called from button click event
        {

            //lets check to see if we are already streaming from the device
            
            if(glob_BluetoothReadTask == null)
            {
                BluetoothReadData bluetoothReadData = new BluetoothReadData();
                //register to recive events and send them to NewBluetoothDataRecieved method
                bluetoothReadData.BluetoothDataRecievedEvent += NewBluetoothDataRecieved; 
                //Start a task to recieve the data
                glob_BluetoothReadTask = Task.Factory.StartNew(() => bluetoothReadData.Run(glob_DeviceAddress), TaskCreationOptions.LongRunning); 
            }
            
            //change the name 
            
        }

        private void NewBluetoothDataRecieved(object sender, SendBluetoothDataArgs args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                textboxYield.Text = args.yieldTime.ToString();
                textboxPaddle.Text = args.paddleTime.ToString();
                StoreTimingData(args.yieldTime, args.paddleTime);
            }), DispatcherPriority.Background);

            
        }

        private void StoreTimingData(float yieldTime, float paddleTime) //This is called when bluetooth data is recieved
        {

            //Do we have a list?
            if (dataToCollectList == null)
            {
                dataToCollectList = new List<DataToCollect>(); //create the list
            }                     

            //get data from the currentGPS class for averaging
            dataToCollectList.Add(new DataToCollect
            {
                savedYieldTime = yieldTime,
                savedPaddleTime = paddleTime,
                savedAlt = currentGPSData.Alt,
                savedSpeedKph = currentGPSData.SpeedKph,
                savedSpeedCMs = currentGPSData.SpeedCMs,
                savedLat = currentGPSData.Lat,
                savedLon = currentGPSData.Lon
            });
    

            //if the list gets to 100 members long then average it and store it for saving
            if (dataToCollectList.Count >= 10)
            {
                int dataToCollectListSize = dataToCollectList.Count;
                float avYieldTime = 0;
                float avPaddleTime = 0;
                float avLat = 0;
                float avLon = 0;
                double avSpeedKph = 0;
                double avSpeedCMs = 0;
                float avAlt = 0;

                //loop through the dataToCollectList and average the results
                for (int i = 0; i <= dataToCollectListSize - 1; i++)
                {
                    avYieldTime = avYieldTime + dataToCollectList[i].savedYieldTime;
                    avPaddleTime = avPaddleTime + dataToCollectList[i].savedPaddleTime;
                    avLat = avLat + dataToCollectList[i].savedLat;
                    avLon = avLon + dataToCollectList[i].savedLon;
                    avSpeedKph = avSpeedKph + dataToCollectList[i].savedSpeedKph;
                    avSpeedCMs = avSpeedCMs + dataToCollectList[i].savedSpeedCMs;
                    avAlt = avAlt + dataToCollectList[i].savedAlt;
                }
                //make them the average
                avYieldTime = avYieldTime / dataToCollectListSize;
                avPaddleTime = avPaddleTime / dataToCollectListSize;
                avLat = avLat / dataToCollectListSize;
                avLon = avLon / dataToCollectListSize;
                avSpeedKph = avSpeedKph / dataToCollectListSize;
                avSpeedCMs = avSpeedCMs / dataToCollectListSize;
                avAlt = avAlt / dataToCollectListSize;

                //if we want to save data to file
                if ((glob_RecordField == true) | (glob_StopRecording == true)) //only save data 
                {                    

                    //put them in new list ready for storage onto file
                    if (dataToCollectForFile == null)
                    {
                        dataToCollectForFile = new List<DataToCollect>();
                    }
                    dataToCollectForFile.Add(new DataToCollect
                    {
                        savedYieldTime = avYieldTime,
                        savedPaddleTime = avPaddleTime,
                        savedLat = avLat,
                        savedLon = avLon,
                        savedSpeedKph = avSpeedKph,
                        savedSpeedCMs = avSpeedCMs,
                        savedAlt = avAlt,
                        savedFieldName = comboboxFields.Text,
                        savedDate = textboxDate.Text,
                        savedTime = textboxTime.Text,
                    });

                    Debug.WriteLine("Stored Record Count: " + dataToCollectForFile.Count);

                    //if we have a lot of records lets save them to a files
                    if ((dataToCollectForFile.Count >= 100) | (glob_StopRecording == true))
                    {                                                
                        radiobuttonFileWrite.IsChecked = true;
                        var dataToCollectForFileProcessing = new List<DataToCollect>(dataToCollectForFile); //create new list so we can clear the main list
                         //register to recive events and send them to NewBluetoothDataRecieved method
                        
                        SaveDataFile saveDataFile = new SaveDataFile();
                        saveDataFile.DataReceivedEvent += NewDataStored;
                        glob_SaveDataToFile = Task.Factory.StartNew(() => saveDataFile.WriteFile(dataToCollectForFileProcessing, dataPath), TaskCreationOptions.LongRunning);

                        dataToCollectForFile.Clear(); //clear the list
                    }
                    
                    //make sure we only save the once after we have asked to stop recording
                    if (glob_StopRecording == true) { glob_StopRecording = false; } 
                }

                //clear the DataToCollectlist
                dataToCollectList.Clear();                
            }
        }

        private void NewDataStored(object sender, SendDataStoredArgs args)
        {
            if (args.dataSaved == true)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    radiobuttonFileWrite.IsChecked = false;
                }), DispatcherPriority.Background);

            }
        }

        //Collect the data and average every 1 second
        private void StartDataCollectTimer() //create the timer Call it to start when the button is started.
        {
            System.Timers.Timer timingsDataCollectionTimer = new System.Timers.Timer();
            timingsDataCollectionTimer.Elapsed += new ElapsedEventHandler(AverageDataAndSave);
            timingsDataCollectionTimer.Interval = 1000;
            timingsDataCollectionTimer.Enabled = true;
        }


        private void AverageDataAndSave(object sender, ElapsedEventArgs e)
        {

        }
       

        //READING DATA FROM BLUETOOTH END________________________________________________________________________________

        //TIMER TO HANDLE TASK MONITORING________________________________________________________________________________
        private void MonitorTasks()
        {
            System.Timers.Timer taskMonitorTimer = new System.Timers.Timer();
            taskMonitorTimer.Elapsed += new ElapsedEventHandler(OnTaskMonitoredEvent);
            taskMonitorTimer.Interval = 500;
            taskMonitorTimer.Enabled = true;
        }

        private void OnTaskMonitoredEvent(object sender, ElapsedEventArgs e)
        {            
            Dispatcher.BeginInvoke(new Action(() =>
            {
            //Date Time Task Status
            if(glob_timeDateTask == null) { radiobuttonDateTime.IsChecked = false; }    
            else if (glob_timeDateTask.Status.ToString() == "Running"){ radiobuttonDateTime.IsChecked = true;}
            else{ radiobuttonDateTime.IsChecked = false;}
            
            if(glob_BluetoothReadTask == null) { radiobuttonBluetoothRead.IsChecked = false; }
            else if (glob_BluetoothReadTask.Status.ToString() == "Running"){radiobuttonBluetoothRead.IsChecked = true;}
            else{ radiobuttonBluetoothRead.IsChecked = false;}

            if (glob_BluetoothScanTask == null) { radiobuttonBluetoothScan.IsChecked = false; }
            else if (glob_BluetoothScanTask.Status.ToString() == "Running") { radiobuttonBluetoothScan.IsChecked = true; }
            else { radiobuttonBluetoothScan.IsChecked = false; }

            if (glob_serialPortTask == null){ radiobuttonSerial.IsChecked = false; }
            else if (glob_serialPortTask.Status.ToString() == "Running") { radiobuttonSerial.IsChecked = true; }
            else { radiobuttonSerial.IsChecked = false; }

            if (glob_SaveDataToFile == null) { radiobuttonFileWrite.IsChecked = false; }

            }), DispatcherPriority.Background);
            
        }

        //TIMER TO HANDLE TASK MONITORING END_____________________________________________________________________________

        //HANDLE TASK UNEXPECTED END EVENTS______________________________________________________________________________
        private void UnexpectedThreadEnd(int whichTaskFailed)
        {
            //TASKS
            //1 = Time Date
            //2 = Serial Port Reading
            //3 = Bluetooth Reading
        }
//HANDLE TASK UNEXPECTED END EVENTS______________________________________________________________________________

    }
}
