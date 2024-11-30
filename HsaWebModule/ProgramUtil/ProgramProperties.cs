using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HsaWebModule
{
    public class ProgramProperties
    {
        public class ProperyList
        {
            public readonly KeyValuePair<int, string> props00 = new KeyValuePair<int, string>(10100, JsonConvert.SerializeObject(new KeyValuePair<string, string>("Log4jLevel", "DEBUG")));
            public readonly KeyValuePair<int, string> props01 = new KeyValuePair<int, string>(10101, JsonConvert.SerializeObject(new KeyValuePair<string, string>("WebSocketPort", "4000")));
            public readonly KeyValuePair<int, string> props02 = new KeyValuePair<int, string>(10102, JsonConvert.SerializeObject(new KeyValuePair<string, string>("HttpServerPort", "5000")));
            public readonly KeyValuePair<int, string> props03 = new KeyValuePair<int, string>(10103, JsonConvert.SerializeObject(new KeyValuePair<string, string>("TCPServerPort", "1470")));
            public readonly KeyValuePair<int, string> props04 = new KeyValuePair<int, string>(10104, JsonConvert.SerializeObject(new KeyValuePair<string, string>("UseWebSocketSSL", "false")));
            public readonly KeyValuePair<int, string> props05 = new KeyValuePair<int, string>(10105, JsonConvert.SerializeObject(new KeyValuePair<string, string>("UseSecureHttp", "false")));
            public readonly KeyValuePair<int, string> props06 = new KeyValuePair<int, string>(10106, JsonConvert.SerializeObject(new KeyValuePair<string, string>("UseAesEncrypt", "true")));
            public readonly KeyValuePair<int, string> props07 = new KeyValuePair<int, string>(10107, JsonConvert.SerializeObject(new KeyValuePair<string, string>("UseZipCompress", "true")));
            public readonly KeyValuePair<int, string> props08 = new KeyValuePair<int, string>(10108, JsonConvert.SerializeObject(new KeyValuePair<string, string>("UseSha256", "true")));
            public readonly KeyValuePair<int, string> props09 = new KeyValuePair<int, string>(10109, JsonConvert.SerializeObject(new KeyValuePair<string, string>("UseJWTEncrypt", "true")));
            public readonly KeyValuePair<int, string> props10 = new KeyValuePair<int, string>(10110, JsonConvert.SerializeObject(new KeyValuePair<string, string>("FileHash", "")));
        }

        public readonly int Log4jLevel = 10100;
        public readonly int WebSocketPort = 10101;
        public readonly int HttpServerPort = 10102;
        public readonly int TCPServerPort = 10103;
        public readonly int UseWebSocketSSL = 10104;
        public readonly int UseSecureHttp = 10105;
        public readonly int UseAesEncrypt = 10106;
        public readonly int UseZipCompress = 10107;
        public readonly int UseSha256 = 10108;
        public readonly int UseJWTEncrypt = 10109;

        public Dictionary<string, string> xmlEntryKvList = new Dictionary<string, string>();
        public string getXmlFilePath = string.Empty;
        public Dictionary<string, int> paramKeyMatchingTable = new Dictionary<string, int>();
        public Dictionary<int, string> paramKeyValueTable = new Dictionary<int, string>();

        public string configFileLocation = Program.programPath + "Config";

        public ProgramProperties()
        {
            string resouceFileName = Program.propertyConfigName;
            if (!Directory.Exists(configFileLocation))
            {
                Directory.CreateDirectory(configFileLocation);
            }
            string messageResourceXmlFilePath = configFileLocation + string.Format(@"\{0}.xml", resouceFileName);

            Program.WriteLog("Create New ConfigFile");

            ProperyList keyValueList = new ProperyList();
            var fields = keyValueList.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                xmlEntryKvList.Add(field.Name, JsonConvert.SerializeObject(field.GetValue(keyValueList)));
            }

            XmlWriterSettings writerSettings = new XmlWriterSettings();
            writerSettings.Indent = true;
            writerSettings.IndentChars = "    ";
            writerSettings.OmitXmlDeclaration = false;
            writerSettings.Encoding = Encoding.UTF8;


            using (XmlWriter xmlWriter = XmlWriter.Create(messageResourceXmlFilePath, writerSettings))
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("properties");
                foreach (var code in xmlEntryKvList)
                {
                    KeyValuePair<string, string> kvStr = JsonConvert.DeserializeObject<KeyValuePair<string, string>>(code.Value);
                    KeyValuePair<string, string> innerValue = JsonConvert.DeserializeObject<KeyValuePair<string, string>>(kvStr.Value);

                    xmlWriter.WriteComment(code.Key);
                    xmlWriter.WriteStartElement("entry");

                    xmlWriter.WriteAttributeString("key", kvStr.Key);
                    xmlWriter.WriteAttributeString("prop", innerValue.Key);
                    xmlWriter.WriteString(innerValue.Value);
                    xmlWriter.WriteEndElement();
                    paramKeyMatchingTable.Add(innerValue.Key, int.Parse(kvStr.Key));
                }
                xmlWriter.WriteEndElement();
                xmlWriter.Flush();
                xmlWriter.Close();
            }

            getXmlFilePath = messageResourceXmlFilePath;
            LoadXmlData();
        }

        public ProgramProperties(string xmlPath)
        {
            getXmlFilePath = xmlPath;
            LoadXmlData();
        }

        public void LoadXmlData()
        {
            Program.WriteLog("Load ConfigFile");

            ProperyList keyValueList = new ProperyList();
            var fields = keyValueList.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                List<Dictionary<string, object>> value = ConvertStringToParams(JsonConvert.SerializeObject(field.GetValue(keyValueList)));
                foreach (Dictionary<string, object> v in value)
                {
                    if (v.ContainsKey("Key"))
                    {
                        if (!paramKeyMatchingTable.ContainsKey(field.Name))
                        {
                            paramKeyMatchingTable.Add(field.Name, Convert.ToInt32(v["Key"]));
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(getXmlFilePath))
            {
                XmlDocument callbackMessageXmlDocument = new XmlDocument();
                callbackMessageXmlDocument.Load(getXmlFilePath);
                XmlNodeList propertyNodeList = callbackMessageXmlDocument.GetElementsByTagName("entry");
                paramKeyValueTable = new Dictionary<int, string>();
                var enumerator = propertyNodeList.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    XmlNode xmlNode = (XmlNode)enumerator.Current;
                    int numKey = Convert.ToInt32(xmlNode.Attributes["key"].Value);
                    paramKeyValueTable.Add(numKey, xmlNode.InnerText);
                }
            }
        }

        private List<Dictionary<string, object>> ConvertStringToParams(string ubformJstr)
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
            if (ubformJstr.StartsWith("["))
            {
                JObject jListInfo = JsonConvert.DeserializeObject<JObject>(ubformJstr);
                List<Dictionary<string, object>> dicList = new List<Dictionary<string, object>>();
                foreach (var jo in jListInfo)
                {
                    dicList.Add(JObject.FromObject(jo).ToObject<Dictionary<string, object>>());
                }
                result = dicList;
            }
            else if (ubformJstr.StartsWith("{"))
            {
                JObject joInfo = JObject.Parse(ubformJstr);
                result.Add(JObject.FromObject(joInfo).ToObject<Dictionary<string, object>>());
            }
            return result;
        }
    }
}
