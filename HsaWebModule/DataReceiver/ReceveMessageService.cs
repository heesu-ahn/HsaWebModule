using System;

namespace HsaWebModule.DataReceiver
{
    public class ReceveMessageService : AbstractAction
    {
        public string socketId = string.Empty;
        public string rowData = string.Empty;
        public string ServerUrl = string.Empty;
        

        public ReceveMessageService(string socketId, string url,string message) 
        {
            this.socketId = socketId;
            this.ServerUrl = url;
            this.rowData = message;
        }

        public override void GetRequestMessage(string socketId, string message)
        {
            _ = CloneableTask(socketId, message).ContinueWith(taskCompletedResult =>
            {
                returnMessage = taskCompletedResult.Result.ToString();
                ExecuteMethodByMessageType(socketId, returnMessage);
            });
        }
        public override void ExecuteMethodByMessageType(string socketId, string message)
        {
            SendResponseMessage(socketId, returnMessage);
        }
        public override void SendResponseMessage(string socketId, string message)
        {
            try
            {
                
            }
            catch (Exception)
            {

                throw;
            }
        }

    }
}
