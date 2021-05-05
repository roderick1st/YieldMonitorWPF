using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace YieldMonitorWPF
{
    class SaveDataFile
    {

        public event DataRecievedEventHandeler DataReceivedEvent;

        public void WriteFile(List<DataToCollect> dataList, string saveDirectory)
        {
            //make a filename
            string fileName = dataList[0].savedFieldName;
            fileName = fileName.Remove(fileName.Length - 9, 9);
            fileName = fileName.Remove(fileName.Length - 5, 1);
            fileName = fileName.Remove(fileName.Length - 7, 1);
            string filePath = saveDirectory + "FieldData/" + fileName + ".xml";

            //see if fiel name exists
            if (File.Exists(filePath))
            {
                Debug.WriteLine("File Exists");
                InsertXmlString(dataList, filePath); // insert data into xml file
            }
            else
            {
                Debug.WriteLine("File not there");
                //check the directory exists
                if(!Directory.Exists(saveDirectory + "\\FieldData"))
                {
                    //directory does not exist
                    Directory.CreateDirectory(saveDirectory + "\\FieldData");
                }
                using (FileStream fs = File.Create(filePath))
                {
                    byte[] xmlHeader = new UTF8Encoding(true).GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
                    fs.Write(xmlHeader, 0, xmlHeader.Length);
                    byte[] xmlNodeStart = new UTF8Encoding(true).GetBytes("<FieldData>\n");
                    fs.Write(xmlNodeStart, 0, xmlNodeStart.Length);
                    byte[] xmlNodeEnd = new UTF8Encoding(true).GetBytes("</FieldData>");
                    fs.Write(xmlNodeEnd, 0, xmlNodeEnd.Length);
                    fs.Close();

                    InsertXmlString(dataList, filePath); //insert data into xml

                }
            }
        }

        private void InsertXmlString(List<DataToCollect> dataList, string filePath)
        {
            //create the xml
            XmlDocument fieldXmlDoc = new XmlDocument();
            fieldXmlDoc.Load(filePath); //open the xml file

            foreach (DataToCollect myReading in dataList)
            {
                //build then data structure in xml
                XmlNode fieldDataNode = fieldXmlDoc.SelectSingleNode("FieldData");

                XmlNode newDataNode = fieldXmlDoc.CreateNode(XmlNodeType.Element, "DataNode", null);

                XmlNode newFieldName = fieldXmlDoc.CreateNode(XmlNodeType.Element, "Field", null);
                newDataNode.AppendChild(newFieldName);
                XmlText newFieldNameData = fieldXmlDoc.CreateTextNode(myReading.savedFieldName);
                newFieldName.AppendChild(newFieldNameData);

                XmlNode newLatReading = fieldXmlDoc.CreateNode(XmlNodeType.Element, "Latitude", null);
                newDataNode.AppendChild(newLatReading);
                XmlText newLatData = fieldXmlDoc.CreateTextNode(myReading.savedLat.ToString());
                newLatReading.AppendChild(newLatData);

                XmlNode newLongReading = fieldXmlDoc.CreateNode(XmlNodeType.Element, "Longitude", null);
                newDataNode.AppendChild(newLongReading);
                XmlText newLongData = fieldXmlDoc.CreateTextNode(myReading.savedLon.ToString());
                newLongReading.AppendChild(newLongData);

                XmlNode newAltReading = fieldXmlDoc.CreateNode(XmlNodeType.Element, "Altitude", null);
                newDataNode.AppendChild(newAltReading);
                XmlText newAltData = fieldXmlDoc.CreateTextNode(myReading.savedAlt.ToString());
                newAltReading.AppendChild(newAltData);

                XmlNode newSpeedReading = fieldXmlDoc.CreateNode(XmlNodeType.Element, "Speed", null);
                newDataNode.AppendChild(newSpeedReading);
                XmlText newSpeedData = fieldXmlDoc.CreateTextNode(myReading.savedSpeedKph.ToString());
                newSpeedReading.AppendChild(newSpeedData);

                XmlNode newYieldReading = fieldXmlDoc.CreateNode(XmlNodeType.Element, "Yield", null);
                newDataNode.AppendChild(newYieldReading);
                XmlText newYieldData = fieldXmlDoc.CreateTextNode(myReading.savedYieldTime.ToString());
                newYieldReading.AppendChild(newYieldData);

                XmlNode newPaddleReading = fieldXmlDoc.CreateNode(XmlNodeType.Element, "Paddle", null);
                newDataNode.AppendChild(newPaddleReading);
                XmlText newPaddleData = fieldXmlDoc.CreateTextNode(myReading.savedPaddleTime.ToString());
                newPaddleReading.AppendChild(newPaddleData);

                XmlNode newDate = fieldXmlDoc.CreateNode(XmlNodeType.Element, "Date", null);
                newDataNode.AppendChild(newDate);
                XmlText newDateData = fieldXmlDoc.CreateTextNode(myReading.savedDate.ToString());
                newDate.AppendChild(newDateData);

                XmlNode newTimeReading = fieldXmlDoc.CreateNode(XmlNodeType.Element, "Time", null);
                newDataNode.AppendChild(newTimeReading);
                XmlText newTimeData = fieldXmlDoc.CreateTextNode(myReading.savedTime.ToString());
                newTimeReading.AppendChild(newTimeData);

                fieldDataNode.AppendChild(newDataNode);

            }

            fieldXmlDoc.Save(filePath); //save record to xml file

            if (DataReceivedEvent != null) //do we have subscribers
            {
                SendDataStoredArgs args = new SendDataStoredArgs() { dataSaved = true };
                DataReceivedEvent.Invoke(null, args);
            }
        }
    }


    public class SendDataStoredArgs : EventArgs
    {
        public bool dataSaved { get; set; }
    }

    public delegate void DataRecievedEventHandeler(object sender, SendDataStoredArgs args);
}