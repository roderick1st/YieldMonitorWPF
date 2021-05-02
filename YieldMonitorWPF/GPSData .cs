using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YieldMonitorWPF
{
    public class GPSData
    {
        public float Lat { get; set; }
        public float Lon { get; set; }
        public double SpeedKph { get; set; }
        public double SpeedCMs { get; set; }
        public float Alt { get; set; }
        public int GpsQuality { get; set; }
        public float PDOP { get; set;}
        public float HDOP { get; set; }
        public float VDOP { get; set; }
    }
}
