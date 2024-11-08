using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace HsaWebModule
{
    public class TestWebSocketServer : TestWebSocketServerBase
    {
        public string address = "127.0.0.1";
        public int port = Program.wsPort;
        public string serviseName = "TestWebSocketServer";
        private string sessionId = "";
        private WebSocket webSocketClient;
        public TestHttpListener parentObj = null;
        private Stream GetStream;
        private byte[] GetBytes;
        public TestWebSocketServer() { manager.ClassName = this.GetType().Name; }

        public TestWebSocketServer(string sessionId, TestHttpListener parent)
        {
            manager.ClassName = this.GetType().Name;
            this.sessionId = sessionId;
            this.parentObj = parent;
            if (webSocketClient != null && webSocketClient.IsAlive)
            {
                webSocketClient.Close();
            }
        }

        public TestWebSocketServer CreateNewObject()
        {
            serviseName = manager.ClassName;
            TestServerOpen(address, port, serviseName);
            return Activator.CreateInstance<TestWebSocketServer>();
        }
        public void TestServerOpen(string address, int port,string serviseName)
        {
            string serverPath = $"ws://{address}:{port}/{serviseName}";
            Console.WriteLine(serverPath);
            webSocketClient = new WebSocket(serverPath);
            webSocketClient.OnMessage += (sender, e) => {
                string message = e.Data;
                Program.WriteLog(message);
                if (!string.IsNullOrEmpty(message) && message.Equals("Close."))
                {
                    webSocketClient.Close();
                    Program.WriteLog("웹소켓 타이머를 재구동 합니다.");
                    Console.WriteLine("");
                    Program.webSocketService.gServer.WebSocketServices["/"].Sessions.SendTo("timerReStart.", parentObj.sessionId);
                    parentObj.Destroy(parentObj.parent,false);
                }
                else
                {
                    Program.webSocketService.gServer.WebSocketServices["/"].Sessions.SendTo(message, parentObj.sessionId);
                    webSocketClient.Send("SendMessageEnd.");
                }
            };
            webSocketClient.OnOpen += (sender, e) => {
                if (Program.webSocketService.gServer.WebSocketServices["/"].Sessions.ActiveIDs.Contains(parentObj.sessionId))
                {
                    CloseParentStream(this.parentObj.CloseStream);
                    Program.WriteLog("TestWebSocketServer가 연결되었습니다.");
                    Program.webSocketService.gServer.WebSocketServices["/"].Sessions.SendTo("TestWebSocketServer가 연결되었습니다.", parentObj.sessionId);
                }
            };
            webSocketClient.OnError += (sender, e) => {
                
            };
            webSocketClient.OnClose += (sender, e) => {

            };
        }
        public void SendMessage(string socketId, string message,Stream stream, byte[] buffer) 
        {
            string[] messageStructure = new string[3]; //socketId,key,value
            messageStructure[0] = sessionId;
            messageStructure[1] = "message";
            messageStructure[2] = message;
            
            this.GetStream = stream;
            this.GetBytes = buffer;

            TestServerOpen(this.address, this.port, this.serviseName);
            if (webSocketClient != null) 
            {
                webSocketClient.Connect();
                if (webSocketClient.IsAlive) 
                { 
                    webSocketClient.Send(String.Join(",",messageStructure));
                }
            }
        }
        public void CloseParentStream(Action<Stream, byte[]> func)
        {
            if (this.GetStream != null && this.GetBytes != null) 
            {
                func(this.GetStream,this.GetBytes);
            }
        }
    }
}
