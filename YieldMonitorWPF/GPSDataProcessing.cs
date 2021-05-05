using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YieldMonitorWPF
{
    class GPSDataProcessing
    {

        public GPSData Process(string gpsDataLine)
        {
            GPSData currentGPSData = new GPSData();

            float fLongitude = -1;
            float fLatitude = -1;
            float fAltitude = -1;
            float fPDOP = -1;
            float fHDOP = -1;
            float fVDOP = -1;
            int iGPSQuality = -1;
            double dSpeed = -1;
            string privateGpsLine = gpsDataLine;
            bool checkSumFailed = true;

            if (privateGpsLine != null)
            {
                if (privateGpsLine.Substring(0, 1) == "$")
                {
                    //get the * location
                    int starLocation = privateGpsLine.IndexOf("*");
                    string msdfgs = starLocation.ToString();
                    //get the check sum
                    //if(privateGpsLine.Substring(starLocation, privateGpsLine.Length - starLocation - 2) != null)
                    if (starLocation > 5)
                    {
                        string checkSum = privateGpsLine.Substring(starLocation + 1, privateGpsLine.Length - starLocation - 3);

                        //get rid of the check sum
                        privateGpsLine = privateGpsLine.Substring(0, privateGpsLine.Length - (privateGpsLine.Length - starLocation));

                        //we have a check sum *
                        checkSumFailed = false;

                        //test checksum against our own calcs
                        if (checkSum != CalculateCheckSum(privateGpsLine).ToString("X"))
                        {
                            checkSumFailed = true;
                        }
                        //string myCheckSum = CalculateCheckSum(privateGpsLine).ToString("X");

                    }

                    //split the string by ,
                    string[] lineData = privateGpsLine.Split(',');

                    //failed checksum needs to amend the begining of the liine
                    if (checkSumFailed)
                    {
                        lineData[0] = "$FAILED";
                    }


                    //get the type of message
                    int iNMEALength = lineData[0].Length;
                    if (iNMEALength >= 6)
                    {
                        iNMEALength = 6;
                    }
                    string myNMEAString = lineData[0].Substring(1, iNMEALength - 1);

                    //convert blanks to -1                
                    for (int lineCount = 0; lineCount > lineData.Length; lineCount++)
                    {
                        if (lineData[lineCount].Length < 1)
                        {
                            lineData[lineCount] = "-1";
                        }
                    }

                    //pull data from the strings. All strings are different
                    switch (myNMEAString)
                    {
                        case "GNGGA":
                            fLatitude = CalculateLatitudeLongitude(myNMEAString, lineData[2], lineData[3], "GGA LAT");
                            fLongitude = CalculateLatitudeLongitude(myNMEAString, lineData[4], lineData[5], "GGA LON");
                            fAltitude = float.Parse(lineData[9]);
                            iGPSQuality = int.Parse(lineData[6]);
                            fHDOP = float.Parse(lineData[8]);
                            break;

                        case "GNGSS":
                            fLatitude = CalculateLatitudeLongitude(myNMEAString, lineData[2], lineData[3], "GSS LAT");
                            fLongitude = CalculateLatitudeLongitude(myNMEAString, lineData[4], lineData[5], "GSS LON");
                            fAltitude = float.Parse(lineData[9]);
                            break;

                        case "GNGNS":
                            fLatitude = CalculateLatitudeLongitude(myNMEAString, lineData[2], lineData[3], "GNS LAT");
                            fLongitude = CalculateLatitudeLongitude(myNMEAString, lineData[4], lineData[5], "GNS LON");
                            fAltitude = float.Parse(lineData[9]);
                            break;

                        case "GLL":
                            fLatitude = CalculateLatitudeLongitude(myNMEAString, lineData[1], lineData[2], "GLL LAT");
                            fLongitude = CalculateLatitudeLongitude(myNMEAString, lineData[3], lineData[4], "GLL LON");
                            break;

                        case "GNRMC":
                            fLatitude = CalculateLatitudeLongitude(myNMEAString, lineData[3], lineData[4], "RMC LAT");
                            fLongitude = CalculateLatitudeLongitude(myNMEAString, lineData[5], lineData[6], "RMC LON");
                            break;

                        case "GNGSA":
                            fPDOP = float.Parse(lineData[3]);
                            fHDOP = float.Parse(lineData[4]);
                            fVDOP = float.Parse(lineData[5]);
                            break;

                        case "GNVTG":
                            dSpeed = double.Parse(lineData[7]);
                            break;

                    }

                    //less than 0.3 kph then change to meters per second
                    if (dSpeed != -1)
                    {
                        currentGPSData.SpeedCMs = Math.Round((dSpeed / 0.036), 0); //cm per second               
                        currentGPSData.SpeedKph = Math.Round(dSpeed, 1); //round to one decimal place
                    }
                    else
                    {
                        currentGPSData.SpeedCMs = -1;
                        currentGPSData.SpeedKph = -1;
                    }

                    //lets put everything into our data holder CurrentGpsData
                    currentGPSData.Lat = fLatitude;
                    currentGPSData.Lon = fLongitude;
                    currentGPSData.Alt = fAltitude;
                    currentGPSData.GpsQuality = iGPSQuality;
                    currentGPSData.PDOP = fPDOP;
                    currentGPSData.HDOP = fHDOP;
                    currentGPSData.VDOP = fVDOP;
                }
            }
                return (currentGPSData);
        }

        private float CalculateLatitudeLongitude(string myNMEAType, string currentLatitudeOrLongitude, string northSouthEastWest, string debugme)
        {
            //(d)dd + (mm.mmmm / 60)(*-1 for W and S)
            int pointPosition = currentLatitudeOrLongitude.IndexOf(".");
            float latlongDegrees = float.Parse(currentLatitudeOrLongitude.Substring(0, pointPosition - 2));
            float latlongMinutes = float.Parse(currentLatitudeOrLongitude.Substring(pointPosition - 2, currentLatitudeOrLongitude.Length - pointPosition + 2));
            float fLatitudeLongitude = latlongDegrees + (latlongMinutes / 60);
            if ((northSouthEastWest == "S") | (northSouthEastWest == "W")) { fLatitudeLongitude = -1 * fLatitudeLongitude; }
 
            return fLatitudeLongitude;
        }

        private int CalculateCheckSum(string nmeaString)
        {
            int checkSum = 0;

            foreach (char myChar in nmeaString)
            {
                if (myChar != '$') {//xor if its not the $ as thats the start of the line
                    if (checkSum == 0)
                    {
                        checkSum = myChar; // if its the first char then lets equal it
                    } else
                    {
                        checkSum = checkSum ^ myChar;
                    }
                }
            }

            return checkSum;
        }

        
    }
}
