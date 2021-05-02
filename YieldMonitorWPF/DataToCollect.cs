using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YieldMonitorWPF
{
    public class DataToCollect
    {
        //public string FieldName { get; set; }
        public float savedYieldTime { get; set; }
        public float savedPaddleTime { get; set; }
        public float savedLat { get; set; }
        public float savedLon { get; set; }
        public double savedSpeedKph { get; set; }
        public double savedSpeedCMs { get; set; }
        public float savedAlt { get; set; }
        public string savedFieldName { get; set; }
        public string savedDate { get; set; }
        public string savedTime { get; set; }
        public int savedGpsQuality { get; set; }
        public float savedPDOP { get; set; }
        public float savedHDOP { get; set; }
        public float savedVDOP { get; set; }
    }
}
