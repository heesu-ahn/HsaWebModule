using System.Collections;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Threading;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Collections.Generic;
using System.Text;
using System.Security.Authentication;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
using System.Security.Claims;
using HsaWebModule.ProgramUtil;
using System.Net;
using HsaWebModule.DataReceiver;
using System.Linq;

namespace HsaWebModule.ProtocolModule
{
    public class WebSocketService
    {
        public Thread gMultiSocketThread;
        public static readonly int gPort = Program.wsPort;
        public static string gServerUrl = "";
        public static int gServerCheckTryCnt = 0;
        public static Hashtable gParamDataSet = new Hashtable();
        public WebSocketServer gServer;
        public static bool gIsOpenListener = false;
        public static bool resetParams = false;
        public static bool skipCurrentProcess = false;
        public static string onMessagData = string.Empty;
        public static string origin = string.Empty; // 서버 연걸 에러 발생시 발생한 서버 주소
        public static Dictionary<string, object> connections; // 멀티요청 처리를 위한 연결된 사용자 관리 객체(socketId)
        
        public class ServerService : WebSocketBehavior
        {
            public InputData GetInputData = new InputData();
            public SendData SendData = new SendData();
            public string secretKey = Guid.NewGuid().ToString();
            protected override void OnMessage(MessageEventArgs e)
            {
                string socketId = this.ID;
                origin = this.Context.Origin;
                if (!string.IsNullOrEmpty(e.Data))
                {
                    onMessagData = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(e.Data));
                }
                try
                {
                    if (connections.ContainsKey(socketId))
                    {
                        var conn = connections[socketId].ToString();
                        if (conn.Contains("Authorization"))
                        {
                            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(conn);
                            if (data.ContainsKey("Authorization"))
                            {
                                string savedJwt = data["Authorization"];
                                savedJwt = savedJwt.Replace("Bearer ", "");
                                
                                string getJwt = string.Empty;
                                if (onMessagData.Contains("Authorization")) 
                                {
                                    Dictionary<string, string> convertMessage = JsonConvert.DeserializeObject<Dictionary<string, string>>(onMessagData);
                                    getJwt = convertMessage["Authorization"].ToString();
                                    getJwt = getJwt.Replace("Bearer ", "");
                                    PreCheckHandler preCheck = new PreCheckHandler(savedJwt, getJwt, secretKey);
                                    var returnJwt = preCheck.AuthTokenCheck();

                                    if (returnJwt.Payload.Iss != null)
                                    {
                                        string currentUser = returnJwt.Payload["userName"].ToString();
                                        string userInfoData = Program.userData.GetUserInfoData();

                                        bool passwordVaildCheck = preCheck.PasswordVaildCheck(convertMessage, currentUser, userInfoData, socketId);
                                        if(passwordVaildCheck) 
                                        {
                                            Program.WriteLog("비밀번호 대칭키 무결성 이상 없음 확인.");
                                            Program.WriteLog(string.Format("유효한 JWT 토큰입니다. 사용자 : {0}",currentUser));
                                            Send("유효한 JWT 토큰입니다.");
                                            string serverUrl = returnJwt.Payload["aud"].ToString();
                                            Console.WriteLine("payload : " + JsonConvert.SerializeObject(returnJwt.Payload));
                                            connections[socketId] = JsonConvert.SerializeObject(new Dictionary<string, string>() { { "serverUrl", serverUrl } });

                                            string address = String.Join(":", serverUrl.Split(':').ToArray().Take(2));
                                            int port = int.Parse(serverUrl.Split(':')[2]);
                                            TestHttpServer httpListenerService = new TestHttpServer(socketId, address, port);
                                        }
                                    }
                                    else 
                                    {
                                        Program.WriteLog("유효하지 않은 JWT 토큰입니다.");
                                        Send("유효하지 않은 JWT 토큰입니다.");
                                        this.Sessions.CloseSession(socketId);
                                    }
                                }
                            }
                        }
                        else 
                        {
                            if (!onMessagData.Contains("Authorization") && conn.Contains("serverUrl"))
                            {
                                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(conn);
                                Console.WriteLine("serverUrl : " + data["serverUrl"]);
                                string serverUrl = data["serverUrl"].ToString();

                                Dictionary<string, object> convertMessage = JsonConvert.DeserializeObject<Dictionary<string, object>>(onMessagData);
                                string message = convertMessage["RequestInfo"].ToString();
                                
                                // 멀티요청을 처리하기 와한 기존 단일 소켓 메시지 처리 과정 전체에 대한 객체 및 비동기 처리화
                                connections[socketId] = message;
                                ReceveMessageService receveMessage = new ReceveMessageService(socketId, serverUrl, message);
                                receveMessage.GetRequestMessage(socketId, receveMessage.rowData);
                            }
                            else 
                            {
                                Program.WriteLog("이미 사용이 만료된 JWT 토큰입니다.");
                                Send("이미 사용이 만료된 JWT 토큰입니다.");
                                this.Sessions.CloseSession(socketId);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Program.WriteLog(ex,true);
                }
            }

            protected override void OnOpen()
            {
                string wss = Program.wsUseSSL ? "wss" : "ws";
                Program.issuer = string.Format("{0}://{1}/", wss, this.Context.Host);
                
                string socketId = this.ID;
                string msg = string.Format("client connected {0}:{1}", Context.UserEndPoint.Address.ToString(), Context.UserEndPoint.Port);
                var claimsIdentity = new List<Claim>()
                {
                    new Claim("id", socketId),
                    new Claim("userName", HsaWebModule.Default.CurrentUserName),
                };
                EncryptModule encryptModule = new EncryptModule();
                string issuer = Program.issuer;
                string audience = Program.audience;
                string result = encryptModule.GenerateJWTToken(claimsIdentity, secretKey, issuer, audience);
                Program.WriteLog("JWT 발급 완료. : " + result);
                string aesEncryptedJwt = Program.encryptModule.EncryptText(result);
                if (connections != null && !connections.ContainsKey(socketId))
                {
                    connections.Add(socketId, JsonConvert.SerializeObject(new Dictionary<string, string>() { { "Authorization", "Bearer " + result } })); // jwt
                }
                else
                {
                    connections[socketId] = JsonConvert.SerializeObject(new Dictionary<string, string>() { { "Authorization", "Bearer " + result } }); // jwt
                }
                Send(JsonConvert.SerializeObject(new Dictionary<string, string>(){{"Authorization","Bearer "+ aesEncryptedJwt } }));
                Program.WriteLog("JWT AES 암호화 완료. : " + aesEncryptedJwt);
            }
            protected override void OnClose(CloseEventArgs e)
            {

            }
            protected override void OnError(ErrorEventArgs e)
            {

            }
        }

        public WebSocketService()
        {
            try
            {
                this.gMultiSocketThread = new Thread(OpenMultiWebSocket);
                this.gMultiSocketThread.Start();
            }
            catch (Exception ex)
            {
                Program.WriteLog(ex, true);
            }
        }

        public void OpenMultiWebSocket()
        {
            try
            {
                connections = new Dictionary<string, object>();

                // Set HttpServerAddress
                string https = Program.wsUseSSL ? "https" : "http";
                
                Program.audience = string.Format("{0}://{1}:{2}", https, IPAddress.Loopback.ToString(), Program.httpPort);
                Console.WriteLine(Program.audience);

                Program.wsUseSSL = true;
                if (Program.wsUseSSL)
                {
                    gServer = new WebSocketServer(port: gPort, secure: true);
                    
                    string certificatePath = Program.certFilePath + string.Format(@"\hsa.pfx");
                    string pasword = "12345";
                    
                    X509Certificate x509 = new X509Certificate(certificatePath, pasword);
                    X509Certificate2 x509Certificate = new X509Certificate2(x509);
                    
                    gServer.SslConfiguration.ServerCertificate = x509Certificate;
                    gServer.SslConfiguration.EnabledSslProtocols = SslProtocols.Default | SslProtocols.Tls11 | SslProtocols.Tls12;
                    gServer.SslConfiguration.ClientCertificateValidationCallback = ValidateClientCertificate;
                }
                else 
                {
                    gServer = new WebSocketServer(port: gPort);
                }
                gServer.AddWebSocketService<ServerService>("/");
                gServer.AddWebSocketService<TestServerService>("/TestWebSocketServer");
                gServer.Start();
            }
            catch (Exception ex)
            {
                Program.WriteLog(ex, true);
            }
        }
        static bool ValidateClientCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            try
            {
                if (sslPolicyErrors != SslPolicyErrors.None)
                {
                    return true;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public class TestServerService : WebSocketBehavior 
        {
            private string plainTextMessage = "";
            protected override void OnOpen() 
            {

            }
            protected override void OnMessage(MessageEventArgs e) 
            {
                string socketId = this.ID;
                string[] messageStructure = new string[3]; //socketId,key,value
                int idx = 0;

                if (!string.IsNullOrEmpty(e.Data))
                {
                    plainTextMessage = e.Data;

                    if (plainTextMessage == "SendMessageEnd.")
                    {
                        Send("Close.");
                    }
                    else 
                    {
                        foreach (var item in plainTextMessage.Split(',').ToArray())
                        {
                            messageStructure[idx++] = item;
                            Console.WriteLine(item);
                        }
                        string sessionId = messageStructure[0];
                        string sendMessage = "{\"type\":\"" + messageStructure[1] + "\",\"message\":\""+ messageStructure[2] + "\"}";
                        Send(sendMessage);
                    }
                }
                else 
                {
                    return;
                }
            }
            protected override void OnClose(CloseEventArgs e)
            {

            }
            protected override void OnError(ErrorEventArgs e)
            {

            }
        }
    }
}
