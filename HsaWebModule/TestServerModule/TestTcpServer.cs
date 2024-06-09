using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HsaWebModule
{
    public class TestTcpServer : TestTcpServerBase
    {
        public int port = Program.tcpPort;
        public bool useConsole = true;
        public int maxSize = 0;
        public int stringLength = (int)Math.Pow(2, 10);
        private string sessionId = "";
        private TestTcpListener tcpListener;
        public TestHttpListener parentObj = null;

        public TestTcpServer(){ manager.ClassName = this.GetType().Name; }
        public TestTcpServer(string sessionId, TestHttpListener parent) 
        { 
            manager.ClassName = this.GetType().Name; 
            this.sessionId = sessionId;
            this.parentObj = parent;
        }

        public TestTcpServer CreateNewObject()
        {
            return Activator.CreateInstance<TestTcpServer>();
        }
        public void TestServerOpen(int port)
        {
            tcpListener = new TestTcpListener(sessionId);
            tcpListener.parent = this;
            tcpListener.SocketOpen(port);
        }
        public void Dispose()
        {
            tcpListener = null;
        }
    }

    public class TestTcpListener 
    {
        public TestTcpServer parent;
        private class AsyncObject
        {
            public byte[] Buffer;
            public Socket WorkingSocket;
            public readonly int BufferSize;
            public AsyncObject(int bufferSize)
            {
                BufferSize = bufferSize;
                Buffer = new byte[(long)BufferSize];
            }
            public void ClearBuffer()
            {
                Array.Clear(Buffer, 0, BufferSize);
            }
        }
        private Socket TcpSocket;
        private List<Socket> connectedClients = new List<Socket>();
        private Socket client;
        public int Port = 0;
        public bool useConsole;
        private string concatString = string.Empty;
        private string sessionId = string.Empty;

        public TestTcpListener(string sessionId) 
        {
            TcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.sessionId = sessionId;
        }
        public TestTcpListener(int port, string sessionId)
        {
            TcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Port = port;
            this.sessionId = sessionId;
        }

        public void SocketOpen(int port) 
        {
            if (TcpSocket != null) 
            {
                IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, port);
                Port = port;
                TcpSocket.Bind(serverEP);
                TcpSocket.Listen(10);
                Program.log.Info($"{parent.manager.className}:SocketOpen.");
                TcpSocket.BeginAccept(AcceptCallback,null);
            }
        }
        
        public void SocketClose(TestTcpServer testTcpServer) 
        {
            try
            {
                foreach (Socket socket in connectedClients)
                {
                    socket.BeginDisconnect(false, DisconnectCallback, null);
                    socket.Disconnect(true);
                    socket.Close(0);
                    socket.Dispose();
                }
                connectedClients.Clear();
                TcpSocket.BeginAccept(AcceptCallback, null);
                Program.log.Info($"{parent.manager.className}:SocketClose.");
            }
            catch (Exception ex)
            {
                Program.log.Error(ex);
            }
        }


        private void DisconnectCallback(IAsyncResult ar)
        {
            
        }

        public void SocketDisconnect()
        {
            try
            {
                if (TcpSocket != null && TcpSocket.Connected)
                {
                    TcpSocket.Disconnect(true);
                    Program.log.Info($"{parent.manager.className}:SocketDisconnect.");
                }
                else 
                {
                    Program.log.Info($"{parent.manager.className} is not connected.");
                }
            }
            catch (Exception ex)
            {
                Program.log.Error(ex);
            }
        }
        public void SocketConnect()
        {
            if (Port != 0) 
            {
                try
                {
                    if (TcpSocket.IsBound)
                    {
                        Program.log.Info($"{parent.manager.className} is Already Started.");
                    }
                    else 
                    {
                        IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, Port);
                        TcpSocket.Bind(serverEP);
                        TcpSocket.Listen(10);
                        Program.log.Info($"{parent.manager.className}:SocketConnect.");
                        TcpSocket.BeginAccept(AcceptCallback, null);
                    }
                }
                catch (Exception ex)
                {
                    Program.log.Error(ex);
                }
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                if (TcpSocket == null)
                {
                    TcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    SocketOpen(Port);
                    SocketConnect();
                }
                else 
                {
                    client = TcpSocket.EndAccept(ar);
                    AsyncObject obj = new AsyncObject(1920 * 1080 * 3);
                    obj.WorkingSocket = client;
                    connectedClients.Add(client);
                    client.BeginReceive(obj.Buffer, 0, 1920 * 1080 * 3, 0, DataReceived, obj);
                }
                
            }
            catch (Exception ex)
            {
                Program.log.Error(ex);
            }
        }
        private void DataReceived(IAsyncResult ar)
        {
            CustomQueue queue = InitializeQueue();

            AsyncObject obj = (AsyncObject)ar.AsyncState;
            client = obj.WorkingSocket;
            int received = client.EndReceive(ar);
            byte[] buffer = new byte[received];
            Array.Copy(obj.Buffer, 0, buffer, 0, received);
            string message = Encoding.UTF8.GetString(buffer);
            if (message.Equals("Quit"))
            {
                Program.log.Info($"{parent.manager.className}:Restart.");
                SocketClose(parent);
                Destroy(parent,sessionId);
                if (parent.parentObj != null) parent.parentObj.Destroy(parent.parentObj.parent,false);
            }
            else 
            {
                string sendMessage = $"{parent.manager.className}:DataReceived. - {message}";
                Program.log.Info(sendMessage);
                sendMessage = "{\"loginIP\":\"127.0.0.1\",\"loginId\":\"myID\",\"Name\":\"myName\",\"NickName\":\"myNickName\",\"password\":\"myPw1234\",\"emaiAddress\":\"myEmail@gmail.com\",\"mobilePhone\":\"82-10-1234-5678\",\"nationality\":\"KOREA\",\"engishTextName\":\"myEnglshName\",\"koreanTextName\":\"나의 한국 이름\",\"gender\":\"Male\",\"dept\":\"laboratory\",\"birthDay\":\"9999-01-01\",\"employmentDate\":\"2222-01-01\",\"age\":\"100\",\"careear\":\"20\",\"weight\":\"55.5\",\"height\":\"166.6\"}";
                queue.Enqueue(sendMessage);
                // setSize
                parent.maxSize = queue.customQueue.MaxCapacity;
                queue.AutoExtractQueue(ExtractString);

                if (sendMessage.Equals(concatString)) 
                {
                    Program.log.Info("SendMessage Validation is Success.");
                }

                SocketClose(parent);
            }
        }

        private CustomQueue InitializeQueue() 
        {
            CustomQueue queue = new CustomQueue(parent.stringLength, 3, true); // 가변 데이타 사이즈 * 가변 큐 사이즈
            //CustomQueue queue = new CustomQueue(3); // 가변 큐 사이즈 1024 * N
            //CustomQueue queue = new CustomQueue(); // 기본 사이즈 1024 * 20
            useConsole = queue.useConsole;

            return queue;
        }
        public void ExtractString(string value)
        {
            if (useConsole) Console.WriteLine($"ExtractString >>> {value}");
            concatString += value;
            client.Send(Encoding.UTF8.GetBytes(value));
        }
        public TestTcpServer Destroy(TestTcpServer tcpServer,string sessionId)
        {
            Program.log.Debug($"Destroy:{tcpServer.manager.className}.");
            tcpServer.manager.ProcessEnd = true;
            if (Program.webSocketService.gServer.WebSocketServices["/"].Sessions.ActiveIDs.Contains(sessionId))
            {
                Program.log.Debug("웹소켓 타이머를 재구동 합니다.");
                Program.webSocketService.gServer.WebSocketServices["/"].Sessions.SendTo("timerReStart.", sessionId);
                Console.WriteLine("");
            }
            else 
            {
                var ids = Program.webSocketService.gServer.WebSocketServices["/"].Sessions.ActiveIDs;
                foreach (var id in ids)
                {
                    Program.webSocketService.gServer.WebSocketServices["/"].Sessions.SendTo("timerReStart.", id);
                }
                Program.log.Debug("웹소켓 타이머를 재구동 합니다.");
                Console.WriteLine("");
            }
            return ((TestTcpServer)tcpServer.manager.ParentObject);
        }
    }
}
