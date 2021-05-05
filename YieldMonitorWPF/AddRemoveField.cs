using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace YieldMonitorWPF
{
    class AddRemoveField
    {
        public void addField(string filePath,string fieldName, string latitude, string longitude,
            string createDate, string createTime)
        {
            if (fieldName.Length < 1)//if the name is less than one char then make a sudo filename
            {
                if (latitude.Length < 5) { latitude = "00.00"; } //just incase we dont have lat
                if (longitude.Length < 5) { longitude = "00.00"; } //just in case we dont have lon
                fieldName = "LAT:" + latitude.Substring(0, 5) + " LON:" + longitude.Substring(0, 5);
            }
            XmlDocument originalXmlDocument = new XmlDocument();
            originalXmlDocument.Load(filePath); //open the xml document

            //build the xml document
            XmlNode fieldsNode = originalXmlDocument.SelectSingleNode("Fields");
            XmlNode newField = originalXmlDocument.CreateNode(XmlNodeType.Element, "Field", null);
            XmlNode newFieldNameElement = originalXmlDocument.CreateNode(XmlNodeType.Element, "name", null);
            XmlText FieldNameText = originalXmlDocument.CreateTextNode(fieldName);
            XmlNode newDate = originalXmlDocument.CreateNode(XmlNodeType.Element, "date", null);
            XmlText dateText = originalXmlDocument.CreateTextNode(createDate);
            XmlNode newTime = originalXmlDocument.CreateNode(XmlNodeType.Element, "time", null);
            XmlText timeText = originalXmlDocument.CreateTextNode(createTime);
            newField.AppendChild(newFieldNameElement);
            newFieldNameElement.AppendChild(FieldNameText);
            newField.AppendChild(newDate);
            newDate.AppendChild(dateText);
            newField.AppendChild(newTime);
            newTime.AppendChild(timeText);
            fieldsNode.AppendChild(newField);

            originalXmlDocument.Save(filePath); //Save the xml file
        }

        public DataSet buildDataSet(string filePath) //build a dataset from the file containing all our fields we have saved
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable("Field");
            ds.Tables.Add(dt);
            dt.Columns.Add("name");
            dt.Columns.Add("date");
            dt.Columns.Add("time");
            foreach (DataColumn dc in dt.Columns)
            {
                dc.ColumnMapping = MappingType.Attribute;
            }
            if (!File.Exists(filePath)) //create the file as its not there
            {
                using (FileStream fs = File.Create(filePath))
                {
                    byte[] xmlHeader = new UTF8Encoding(true).GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
                    fs.Write(xmlHeader, 0, xmlHeader.Length);
                    byte[] xmlNodeStart = new UTF8Encoding(true).GetBytes("<Fields>\n");
                    fs.Write(xmlNodeStart, 0, xmlNodeStart.Length);
                    byte[] xmlNodeContent = new UTF8Encoding(true).GetBytes(
                        "\t<Field>" +
                        "\t\t<name>NO FIELD</name>\n" +
                        "\t\t<date>00/00/0000</date>\n" +
                        "\t\t<time>00:00:00</time>\n" +
                        "\t</Field>");
                    fs.Write(xmlNodeContent, 0, xmlNodeContent.Length);
                    byte[] xmlNodeEnd = new UTF8Encoding(true).GetBytes("</Fields>");
                    fs.Write(xmlNodeEnd, 0, xmlNodeEnd.Length);
                    fs.Close();                   
                }
            }
            FileStream myFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            StreamReader myXmlStream = new StreamReader(myFileStream);
            ds.ReadXml(myXmlStream);
            myFileStream.Close();
            return (ds);//return the data set
        }
    }
}
