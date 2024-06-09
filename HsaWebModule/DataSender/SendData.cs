using System;
using System.Threading;
using static HsaWebModule.InputData;
using HsaWebModule.DataReceiver;

namespace HsaWebModule
{
    public class SendData
    {
        public Timer thrdTimer;
        public AutoResetEvent autoReset;
        public StatusChecker statusChecker;
        public InputData inputData;
        public string serverUrl = string.Empty;
        public string socketId;
        private bool timerStart = true;

        public event EventHandler IsTimeOver;
        public bool TimeOver
        {
            get { return statusChecker.checkEnd; }
            set
            {
                statusChecker.checkEnd = value;
                CheckAndCallHandlers();
            }
        }
        private void CheckAndCallHandlers()
        {
            try
            {
                EventHandler handler = IsTimeOver;
                if (statusChecker.checkEnd)
                {
                    timerStart = true;
                    thrdTimer.Dispose();
                    Send(socketId,inputData);
                }
            }
            catch (Exception ex)
            {
                Program.log.Debug(ex.Message);
            }
        }

        public SendData()
        {
            try
            {
                autoReset = new AutoResetEvent(false);
                statusChecker = new StatusChecker(5);
            }
            catch (Exception ex)
            {
                Program.log.Debug(ex.Message);
            }
        }
        public void SetTimeOut(string id, InputData data) 
        {
            try
            {
                socketId = id;
                Thread timeout = new Thread(TimerStart);
                timeout.IsBackground = true;
                timeout.Start();
            }
            catch (Exception ex)
            {
                Program.log.Debug(ex.Message);
            }
        }

        private void TimerStart()
        {
            try
            {
                if (timerStart)
                {
                    thrdTimer = new System.Threading.Timer(statusChecker.CheckStatus, autoReset, 500, 100);
                    timerStart = false;
                }
                autoReset.WaitOne();
                TimeOver = statusChecker.checkEnd;
            }
            catch (Exception ex)
            {
                Program.log.Debug(ex.Message);
            }
        }

        public void Send(string socketId,InputData data)
        {
            Program.log.Debug("Send : " +  data.inputData.strings.Count);
            data.inputData.strings.ForEach(message => {
                ReceveMessageService receveMessage = new ReceveMessageService(socketId, serverUrl, message);
                receveMessage.GetRequestMessage(socketId, receveMessage.rowData);
            });
            inputData.inputData.strings = new inputDataList().Clone();
        }
    }
}
