using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace YieldMonitorWPF
{
    class ProgramSettings
    {
        //the program has closed so lets store last settings in LastSettings.mxl
        public void SaveSettings(string filePath, string fieldName, int fieldComboBoxIndex, int GPSComboIndex)
        {
            string fileLocation = filePath + "LastSettings.xml";

            //check file exists
            if (File.Exists(fileLocation))
            {
                //delete the file
                File.Delete(fileLocation);
            }

            //create new xml file
            using (FileStream fs = File.Create(fileLocation))
            {
                byte[] xmlHeader = new UTF8Encoding(true).GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
                fs.Write(xmlHeader, 0, xmlHeader.Length);
                byte[] xmlNodeSettingsStart = new UTF8Encoding(true).GetBytes("<Settings>\n");
                fs.Write(xmlNodeSettingsStart, 0, xmlNodeSettingsStart.Length);
                byte[] xmlNodeField = new UTF8Encoding(true).GetBytes("\t<Field>" + fieldName + "</Field>\n");
                fs.Write(xmlNodeField, 0, xmlNodeField.Length);
                byte[] xmlNodeFieldCombo = new UTF8Encoding(true).GetBytes("\t<ComboIndex>" + fieldComboBoxIndex + "</ComboIndex>\n");
                fs.Write(xmlNodeFieldCombo, 0, xmlNodeFieldCombo.Length);
                byte[] xmlNodeGPSCombo = new UTF8Encoding(true).GetBytes("\t<GPSPort>" + GPSComboIndex + "</GPSPort>\n");
                fs.Write(xmlNodeGPSCombo, 0, xmlNodeGPSCombo.Length);
                byte[] xmlNodeSettingsEnd = new UTF8Encoding(true).GetBytes("</Settings>");
                fs.Write(xmlNodeSettingsEnd, 0, xmlNodeSettingsEnd.Length);
                fs.Close();
            }
        }

        public string LoadSettings(string filePath, string fileName)
        {
            string fullFilePath = filePath + "//" + fileName;           
            XmlDocument doc = new XmlDocument();
            doc.Load(fullFilePath);
            XmlNode node = doc.DocumentElement.SelectSingleNode("/Settings/Field");
            string fieldLastUsed = node.InnerText;
            return fieldLastUsed;
        }

    }
}
