using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace HsaWebModule
{
    public class TestHttpServer : TestHttpServerBase
    {
        public string address = "http://127.0.0.1";
        public int port = 5000;
        
        private TestHttpListener httpListener;
        public TestHttpServer(){ manager.ClassName = this.GetType().Name; }

        public TestHttpServer(string sessionId, string address, int port, string path = "", string serviceName = "") 
        {
            httpListener = new TestHttpListener(sessionId);
            httpListener.parent = this;
            manager.ClassName = this.GetType().Name;
            this.address = address;
            this.port = port;
            httpListener.TestServerOpen(this.address, this.port);
        }
        public TestHttpServer CreateNewObject()
        {
            return Activator.CreateInstance<TestHttpServer>();
        }
    }

    public class TestHttpListener 
    {
        public TestHttpServer parent;
        private readonly List<string> prefix = new List<string>();
        private HttpListener httpListener = new HttpListener();
        public bool stopServer = false;
        public HttpListener sender = null;

        public string path = "HsaWebModule";
        public string serviceName = "Service.do";
        public string sessionId = "";
        public bool wsServiceEnd = false;

        public TestHttpListener(string sessionId) 
        {
            this.sessionId = sessionId;
            if (httpListener != null) 
            {
                HttpListenerTimeoutManager manager = httpListener.TimeoutManager;
                manager.IdleConnection = TimeSpan.FromMinutes(5);
                manager.HeaderWait = TimeSpan.FromMinutes(5);
            }
        }

        public void TestServerOpen(string address, int port)
        {
            if (address == "")
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    address = endPoint.Address.ToString();
                }
            }
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();
            var inUse = ipEndPoints.Where(w => w.Port == Program.httpPort).FirstOrDefault();
            if (inUse == null)
            {
                string serverPath = $"{address}:{port}/";
                Program.WriteLog(serverPath);
                prefix.Add(serverPath);
                foreach (string s in prefix)
                {
                    httpListener.Prefixes.Add(s);
                }
                ListenMessage(httpListener);
            }
            else 
            {
                httpListener = new HttpListener();
                HttpListenerTimeoutManager manager = httpListener.TimeoutManager;
                manager.IdleConnection = TimeSpan.FromMinutes(5);
                manager.HeaderWait = TimeSpan.FromMinutes(5);

                string serverPath = $"{address}:{port}/";
                Program.WriteLog(serverPath);
                prefix.Add(serverPath);
                foreach (string s in prefix)
                {
                    httpListener.Prefixes.Add(s);
                }
                ListenMessage(httpListener);
            }
        }

        private async void ListenMessage(HttpListener listener)
        {
            Program.WriteLog("HttpListenerService listenMessage");
            try
            {
                httpListener.Start();

                while (true)
                {
                    if (stopServer)
                    {
                        break;
                    }
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    Stream output = null;

                    string documentContents;
                    string responseString = "";

                    if (request.HttpMethod != "")
                    {
                        Program.WriteLog(string.Format("Method : {0}", request.HttpMethod));

                        Program.WriteLog($"Recived request for {request.Url}");
                        if (request.HttpMethod == "OPTIONS") return;
                        if (request.RawUrl != "/HsaWebModule/Service.do") return;

                        using (Stream receiveStream = request.InputStream)
                        {
                            using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                            {
                                documentContents = readStream.ReadToEnd();
                            }
                        }
                        string paremeter = HttpUtility.UrlDecode(documentContents);
                        string[] paramList;
                        if (paremeter.Split('&').Length > 1)
                        {
                            paramList = paremeter.Split('&');
                        }

                        NameValueCollection nvcData = HttpUtility.ParseQueryString(documentContents);
                        Dictionary<string, string> dictData = new Dictionary<string, string>(nvcData.Count);
                        foreach (string key in nvcData.AllKeys)
                        {
                            dictData.Add(key, nvcData.Get(key));
                        }
                        Program.WriteLog(string.Format("parameter : {0}", paremeter));
                        HttpListenerResponse response = context.Response;
                        response.StatusCode = 200;
                        response.ContentType = "Application/json";
                        response.AddHeader("Access-Control-Allow-Origin", "*");
                        response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS, PUT, PATCH, DELETE");
                        response.AddHeader("Access-Control-Allow-Headers", "X-Requested-With,Content-Type', 'Access-Control-Allow-Origin', 'Origin");
                        response.AddHeader("Access-Control-Allow-Credentialistener", "true");
                        
                        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                        response.ContentLength64 = buffer.Length;

                        if (dictData.ContainsKey("REQUEST_TYPE"))
                        {
                            string REQUEST_TYPE = dictData["REQUEST_TYPE"].ToString();

                            if (REQUEST_TYPE.Equals("TestTcpServer"))
                            {
                                IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                                IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();
                                var inUse = ipEndPoints.Where(w => w.Port == Program.tcpPort).FirstOrDefault();
                                if (inUse == null) 
                                {
                                    TestTcpServer tcpServer = new TestTcpServer(sessionId,this);
                                    tcpServer.TestServerOpen(tcpServer.port);                                    
                                }
                            }
                            else if (REQUEST_TYPE.Equals("TestHttpServer"))
                            {
                                string encryptPassword = $"value&{Convert.ToBase64String(Encoding.UTF8.GetBytes(dictData["password"].ToString()))}";
                                string cookieString = $"{Program.defaultUserName}={encryptPassword};Domain=localhost;Path=/;Max-Age={1800};Secure;";
                                responseString = "{\"result\":" + "\"" + REQUEST_TYPE + " Open Success." + "\",\"Set-Cookie\":" + "\"" + cookieString + "\"}";
                                buffer = Encoding.UTF8.GetBytes(responseString);
                                response.ContentLength64 = buffer.Length;
                                output = response.OutputStream;
                                await output.WriteAsync(buffer, 0, buffer.Length);
                                output.Close();
                            }
                            else if (REQUEST_TYPE.Equals("TestWebSocketServer"))
                            {
                                string encryptPassword = $"value&{Convert.ToBase64String(Encoding.UTF8.GetBytes(dictData["password"].ToString()))}";
                                string cookieString = $"{Program.defaultUserName}={encryptPassword};Domain=localhost;Path=/;Max-Age={1800};Secure;";
                                responseString = "{\"result\":" + "\"" + REQUEST_TYPE + " Open Success." + "\",\"Set-Cookie\":" + "\"" + cookieString + "\"}";
                                buffer = Encoding.UTF8.GetBytes(responseString);
                                response.ContentLength64 = buffer.Length;
                                output = response.OutputStream;
                                TestWebSocketServer webSocketServer = new TestWebSocketServer(sessionId,this);
                                webSocketServer.SendMessage(sessionId, "Send Message Test Success.",output,buffer);
                            }
                            
                            sender = listener;

                            if (REQUEST_TYPE.Equals("TestHttpServer"))
                            {
                                Destroy(parent);
                                HttpListenerPrefixCollection prefixes = listener.Prefixes;
                                prefixes.Clear();
                                stopServer = true;
                            }
                            else 
                            {
                                HttpListenerPrefixCollection prefixes = listener.Prefixes;
                                prefixes.Clear();
                                stopServer = true;
                            }
                        }
                        else
                        {
                            output = response.OutputStream;
                            output.Write(buffer, 0, buffer.Length);
                            output.Close();
                            Destroy(parent);
                            HttpListenerPrefixCollection prefixes = listener.Prefixes;
                            prefixes.Clear();
                            stopServer = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.WriteLog(ex, true);
            }
        }
        public TestHttpServer Destroy(TestHttpServer httpServer,bool timerRestart = true)
        {
            Program.WriteLog($"Destroy:{httpServer.manager.className}.");
            httpServer.manager.ProcessEnd = true;

            if (timerRestart)
            {
                Program.WriteLog("웹소켓 타이머를 재구동 합니다.");
                Program.webSocketService.gServer.WebSocketServices["/"].Sessions.SendTo("timerReStart.", sessionId);
                Program.WriteLog("");
            }
            return ((TestHttpServer)httpServer.manager.ParentObject);
        }

        public void CloseStream(Stream stream, byte[] buffer) 
        {
            if (stream != null && buffer != null && buffer.Length > 0) 
            {
                Program.WriteLog("CloseStream From Other Class.");
                stream.WriteAsync(buffer, 0, buffer.Length).Wait();
                stream.Close();
            }
        }
    }
}
