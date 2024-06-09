using HsaWebModule.ProtocolModule;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.Forms;
using HsaWebModule.ProgramUtil;
using HsaWebModule.Screen;
using Newtonsoft.Json;

namespace HsaWebModule
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        public static ILog log = null;
        public static string programPath = new DirectoryInfo(Application.StartupPath).Parent.Parent.FullName;//Application.StartupPath;
        public static string propertyConfigName = "HsaWebModule.Property";
        public static Dictionary<int,Dictionary<string,string>> Properties = new Dictionary<int, Dictionary<string, string>>();
        public static ProgramProperties keyCodeTable = null;
        public static string log4jLevel = string.Empty;
        public static bool wsUseSSL = false;
        public static string certFilePath = string.Empty;
        public static int wsPort = 0;
        public static int httpPort = 0;
        public static int tcpPort = 0;
        public static WebSocketService webSocketService;
        public static UserDataModule userData;
        public static EncryptModule encryptModule = new EncryptModule();
        public static string issuer = string.Empty;
        public static string audience = "http://127.0.0.1:8080";
        public static TrayIcon trayIcon;
        public static string defaultUserName = "hsaWebModule"; 

        public delegate void runServiceModule();
        public static Dictionary<string, string> TestServerType = new Dictionary<string, string>();
        
        [STAThread]
        static void Main()
        {
            programPath = programPath + @"\";
            string fileHashResult = string.Empty;
            string resouceFileName = propertyConfigName;
            string messageResourceXmlFilePath = programPath + string.Format(@"Config\{0}.xml", resouceFileName);
            if (File.Exists(messageResourceXmlFilePath))
            {
                // 있으면 기존 xml 파일에서 로드한다
                // 파일 위변조 방지
                string checkFileHash = encryptModule.GetFileHash(programPath + "Config");
                if (checkFileHash != HsaWebModule.Default.PropertyFileKey)
                {
                    fileHashResult = "설정 파일 변조 확인. 삭제 후 설정파일 재생성.";
                    // 파일키가 다르면 지우고 다시 만든다
                    File.Delete(messageResourceXmlFilePath);
                    ProgramProperties programProperties = new ProgramProperties();

                    checkFileHash = encryptModule.GetFileHash(programPath + "Config");
                    HsaWebModule.Default.PropertyFileKey = checkFileHash;
                    HsaWebModule.Default.Save();

                    foreach (var key in programProperties.paramKeyMatchingTable.Keys)
                    {
                        int numKey = programProperties.paramKeyMatchingTable[key];
                        string value = programProperties.paramKeyValueTable[numKey];
                        if (!Properties.ContainsKey(numKey))
                        {
                            Properties.Add(numKey, new Dictionary<string, string>() { { key, value } });
                        }
                    }
                    keyCodeTable = programProperties;
                }
                else 
                {
                    fileHashResult = "설정 파일 동일성 확인.";
                    ProgramProperties programProperties = new ProgramProperties(messageResourceXmlFilePath);
                    foreach (var key in programProperties.paramKeyMatchingTable.Keys)
                    {
                        int numKey = programProperties.paramKeyMatchingTable[key];
                        string value = programProperties.paramKeyValueTable[numKey];
                        Properties.Add(numKey, new Dictionary<string, string>(){{ key, value}});
                    }
                    keyCodeTable = programProperties;
                }

            }
            else
            {
                ProgramProperties programProperties = new ProgramProperties();

                EncryptModule encryptModule = new EncryptModule();
                string checkFileHash = encryptModule.GetFileHash(programPath + "Config");
                HsaWebModule.Default.PropertyFileKey = checkFileHash;
                HsaWebModule.Default.Save();

                foreach (var key in programProperties.paramKeyMatchingTable.Keys)
                {
                    int numKey = programProperties.paramKeyMatchingTable[key];
                    string value = programProperties.paramKeyValueTable[numKey];
                    if (!Properties.ContainsKey(numKey)) 
                    {
                        Properties.Add(numKey, new Dictionary<string, string>() { { key, value } });
                    }
                }
                keyCodeTable = programProperties;
            }

            wsPort = int.Parse(Properties[keyCodeTable.WebSocketPort].ToArray()[0].Value);
            httpPort = int.Parse(Properties[keyCodeTable.HttpServerPort].ToArray()[0].Value);
            tcpPort = int.Parse(Properties[keyCodeTable.TCPServerPort].ToArray()[0].Value);

            log4jLevel = Properties[keyCodeTable.Log4jLevel].ToArray()[0].Value;
            if (!Directory.Exists(programPath + "Log"))
            {
                Directory.CreateDirectory(programPath + "Log");
            }

            Log4netManager log4NetManager = new Log4netManager(programPath + @"Log\", log4jLevel);
            log = log4NetManager.SetLogger();
            log.Debug(fileHashResult); // 설정 파일 동일성 검사 결과

            Application.EnableVisualStyles();            
            
            TestServerType.Add("Tcp", "TestTcpServer");
            TestServerType.Add("Http", "TestHttpServer");
            TestServerType.Add("WebSocket", "TestWebSocketServer");
            log.Debug($"{Application.ProductName}:Start.");
            
            if (!checkExistLoad())
            {
                log.Debug("Program is already running.");
                return;
            }
            log.Debug("Program Started");

            wsUseSSL = bool.Parse(Properties[keyCodeTable.UseWebSocketSSL].ToArray()[0].Value);
            runServiceModule serviceStart = new runServiceModule(WebSocketServiceStart); //Appication
            serviceStart();
            
            trayIcon = new TrayIcon();
            string userName = string.Empty;
            string userInfoData = new UserDataModule(certFilePath).GetUserInfoData();
            Dictionary<string, string> userInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(userInfoData);
            if (userInfo.ContainsKey(HsaWebModule.Default.CurrentUserName))
            {
                userName = HsaWebModule.Default.CurrentUserName;
                userName = " " + userName + " 님";
            }
            trayIcon.notifyIcon.BalloonTipTitle = Application.ProductName + "구동 성공 알림 메시지";
            trayIcon.notifyIcon.BalloonTipText = string.Format("안녕하세요{0}", userName);
            trayIcon.notifyIcon.ShowBalloonTip(500);
            
            Application.Run();
        }

        private static void WebSocketServiceStart()
        {
            try
            {
                certFilePath = programPath + "keyFiles";
                if (wsUseSSL) 
                {
                    InstallCertificate(StoreLocation.CurrentUser, StoreName.Root, certFilePath + string.Format(@"\hsa.crt"));
                    InstallCertificate(StoreLocation.CurrentUser, StoreName.TrustedPublisher, certFilePath + string.Format(@"\hsa.crt"));
                }
                webSocketService = new WebSocketService();
                UserDataModule userDataModule = new UserDataModule(certFilePath);
                userDataModule.CreateUserInfoData();
                userDataModule = null;
                userDataModule = new UserDataModule(certFilePath);
                userData = userDataModule;
            }
            catch (Exception ex)
            {
                log.Debug(ex);
            }
        }

        private static void InstallCertificate(StoreLocation storeLocation, StoreName storeName, string certFilePath)
        {
            try
            {
                X509Store store = new X509Store(storeName, storeLocation);
                store.Open(OpenFlags.ReadWrite);
                X509Certificate2 certificate = new X509Certificate2(X509Certificate2.CreateFromCertFile(certFilePath));
                certificate.FriendlyName = "hsa";
                store.Add(certificate);
                store.Close();
            }
            catch (Exception ex)
            {
                log.Debug(ex);
            }
        }

        private static bool checkExistLoad()
        {
            // GUID 대신 사용자 임의대로 뮤텍스 이름 사용
            string mtxName = Process.GetCurrentProcess().ProcessName; //"LocalExporter";
            bool isAvailable = true;
            Mutex mtx = new Mutex(true, mtxName);

            // 1초 동안 뮤텍스를 획득하려 대기  
            TimeSpan tsWait = new TimeSpan(0, 0, 1);
            if (mtx.WaitOne(tsWait))
            {
                Process[] procs = Process.GetProcessesByName(mtxName);
                if (procs.Length > 1) isAvailable = false;
            }
            else isAvailable = false;
            return isAvailable;
        }
    }
}
