using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HsaWebModule
{
    public abstract class AbstractAction
    {
        public string socketId;
        public string rowData;
        public string returnMessage;
        public TestHttpServer httpListenerService;

        public abstract void GetRequestMessage(string socketId, string message);
        public abstract void ExecuteMethodByMessageType(string socketId, string message);
        public abstract void SendResponseMessage(string socketId, string message);

        public AbstractAction() 
        {

        }

        public delegate string cloneableDelegate(string socketId, string message);
        public async Task<string> CloneableTask(string socketId, string message)
        {
            string returnResult;
            cloneableDelegate originDelegate = new cloneableDelegate(abstractAction);
            IAsyncResult asyncRes = originDelegate.BeginInvoke(socketId, message, null, null);
            returnResult = originDelegate.EndInvoke(asyncRes);
            return returnResult;
        }
        public string abstractAction(string socketId, string message)
        {
            string result = string.Empty;
            try
            {
                Dictionary<string, object> paramData = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
                if (paramData != null) 
                {
                    if (httpListenerService != null) 
                    {
                        //result = httpListenerService.LoadData(paramData).Result;
                    } 
                }
                Program.WriteLog(string.Format("session id : {0} TaskCompleted : {1}", socketId, result));
            }
            catch (Exception ex)
            {
                Program.log.Error(ex);
            }
            return result;
        }


        internal List<Dictionary<string, object>> ConvertStringToParams(string jsonString = "")
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
            if (string.IsNullOrEmpty(jsonString))
            {
                return result;
            }
            else 
            {
                if (jsonString.StartsWith("["))
                {
                    List<JObject> jListInfo = JsonConvert.DeserializeObject<List<JObject>>(jsonString);
                    List<Dictionary<string, object>> dicList = new List<Dictionary<string, object>>();
                    foreach (var jo in jListInfo)
                    {
                        dicList.Add(JObject.FromObject(jo).ToObject<Dictionary<string, object>>());
                    }
                    result = dicList;
                }
                else if (jsonString.StartsWith("{"))
                {
                    JObject joInfo = JObject.Parse(jsonString);
                    result.Add(JObject.FromObject(joInfo).ToObject<Dictionary<string, object>>());
                }
                return result;
            }
        }
    }
}
