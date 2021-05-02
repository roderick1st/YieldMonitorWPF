using System;
using System.Collections.Generic;
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

            if (gpsDataLine != null)
            {
                //split the string by ,
                string[] lineData = gpsDataLine.Split(',');

                //get the type of message
                string myNMEAString = lineData[0].Substring(3, 3);

                //we should check the checksum here
                //NOT DONE

                //pull data from the strings. All strings are different
                switch (myNMEAString)
                {
                    case "GGA":
                        fLatitude = CalculateLatitudeLongitude(myNMEAString, lineData[2], lineData[3]);
                        fLongitude = CalculateLatitudeLongitude(myNMEAString, lineData[4], lineData[5]);
                        fAltitude = float.Parse(lineData[9]);
                        iGPSQuality = int.Parse(lineData[6]);
                        break;

                    case "GLL":
                        fLatitude = CalculateLatitudeLongitude(myNMEAString, lineData[1], lineData[2]);
                        fLongitude = CalculateLatitudeLongitude(myNMEAString, lineData[3], lineData[4]);
                        break;

                    case "RMC":
                        fLatitude = CalculateLatitudeLongitude(myNMEAString, lineData[3], lineData[4]);
                        fLongitude = CalculateLatitudeLongitude(myNMEAString, lineData[5], lineData[6]);
                        break;

                    case "GSA":
                        fPDOP = float.Parse(lineData[15]);
                        fHDOP = float.Parse(lineData[16]);
                        fVDOP = float.Parse(lineData[17]);
                        break;

                    case "VTG":
                        dSpeed = double.Parse(lineData[7]);
                        break;

                }

                //less than 0.3 kph then change to meters per second
                if(dSpeed != -1)
                {
                    currentGPSData.SpeedCMs = Math.Round((dSpeed / 0.036),0); //cm per second               
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
            return (currentGPSData);
        }

        private float CalculateLatitudeLongitude(string myNMEAType, string currentLatitudeOrLongitude, string northSouthEastWest)
        {
            //(d)dd + (mm.mmmm / 60)(*-1 for W and S)
            int pointPosition = currentLatitudeOrLongitude.IndexOf(".");
            float latlongDegrees = float.Parse(currentLatitudeOrLongitude.Substring(0, pointPosition - 2));
            float latlongMinutes = float.Parse(currentLatitudeOrLongitude.Substring(pointPosition - 2, currentLatitudeOrLongitude.Length - pointPosition + 2));
            float fLatitudeLongitude = latlongDegrees + (latlongMinutes / 60);
            if ((northSouthEastWest == "S") | (northSouthEastWest == "W")) { fLatitudeLongitude = -1 * fLatitudeLongitude; }
 
            return fLatitudeLongitude;
        }

        
    }
}
