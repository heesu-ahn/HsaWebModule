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
        public static ILog log;
        public static string programPath = HsaWebModuleProperty.mainProperty.programPath;
        public static string propertyConfigName = HsaWebModuleProperty.mainProperty.propertyConfigName;
        public static Dictionary<int,Dictionary<string,string>> Properties = HsaWebModuleProperty.mainProperty.Properties;
        public static ProgramProperties keyCodeTable = HsaWebModuleProperty.mainProperty.keyCodeTable;
        public static string log4jLevel = HsaWebModuleProperty.mainProperty.log4jLevel;
        public static bool wsUseSSL = HsaWebModuleProperty.mainProperty.wsUseSSL;
        public static string certFilePath = HsaWebModuleProperty.mainProperty.certFilePath;
        public static int wsPort = HsaWebModuleProperty.mainProperty.wsPort;
        public static int httpPort = HsaWebModuleProperty.mainProperty.httpPort;
        public static int tcpPort = HsaWebModuleProperty.mainProperty.tcpPort;
        public static WebSocketService webSocketService;
        public static UserDataModule userData;
        public static EncryptModule encryptModule;
        public static string issuer = HsaWebModuleProperty.mainProperty.issuer;
        public static string audience = HsaWebModuleProperty.mainProperty.audience;
        public static TrayIcon trayIcon;
        public static string defaultUserName = HsaWebModuleProperty.mainProperty.defaultUserName; 

        public delegate void runServiceModule();
        public static Dictionary<string, string> TestServerType;
        public static bool IsDebugMode { get; set; }

        [STAThread]
        static void Main()
        {
            HsaWebModuleProperty.mainProperty.TestServerType = new Dictionary<string, string>();
            HsaWebModuleProperty.mainProperty.encryptModule = new EncryptModule();
            HsaWebModuleProperty.mainProperty.programPath = programPath + @"\";

            string fileHashResult = string.Empty;
            string resouceFileName = propertyConfigName;
            string messageResourceXmlFilePath = $"{programPath}{string.Format(@"Config\{0}.xml", HsaWebModuleProperty.mainProperty.propertyConfigName)}";
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

            HsaWebModuleProperty.mainProperty.wsPort = int.Parse(Properties[keyCodeTable.WebSocketPort].ToArray()[0].Value);
            HsaWebModuleProperty.mainProperty.httpPort = int.Parse(Properties[keyCodeTable.HttpServerPort].ToArray()[0].Value);
            HsaWebModuleProperty.mainProperty.tcpPort = int.Parse(Properties[keyCodeTable.TCPServerPort].ToArray()[0].Value);
            HsaWebModuleProperty.mainProperty.log4jLevel = Properties[keyCodeTable.Log4jLevel].ToArray()[0].Value;

            if (!Directory.Exists(programPath + "Log"))
            {
                Directory.CreateDirectory(programPath + "Log");
            }

            IsDebugMode = isDebugging(); 

            Log4netManager log4NetManager = new Log4netManager(programPath + @"Log\", log4jLevel);
            HsaWebModuleProperty.mainProperty.log = log4NetManager.SetLogger();

            WriteLog($"is Degbug Mode : {IsDebugMode}.");
            WriteLog(fileHashResult); // 설정 파일 동일성 검사 결과


            Application.EnableVisualStyles();            
            
            TestServerType.Add("Tcp", "TestTcpServer");
            TestServerType.Add("Http", "TestHttpServer");
            TestServerType.Add("WebSocket", "TestWebSocketServer");
            WriteLog($"{Application.ProductName}:Start.");
            
            if (!checkExistLoad())
            {
                WriteLog("Program is already running.");
                return;
            }
            WriteLog("Program Started");

            HsaWebModuleProperty.mainProperty.wsUseSSL = bool.Parse(Properties[keyCodeTable.UseWebSocketSSL].ToArray()[0].Value);
            runServiceModule serviceStart = new runServiceModule(WebSocketServiceStart); //Appication
            serviceStart();
            
            HsaWebModuleProperty.mainProperty.trayIcon = new TrayIcon();
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
                WriteLog(ex,true);
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
                WriteLog(ex, true);
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


        public static bool isDebugging()
        {
            bool debugging = false;
            WellAreWe(ref debugging);
            return debugging;
        }
        [Conditional("DEBUG")]
        private static void WellAreWe(ref bool debugging)
        {
            debugging = true;
        }
        public static void WriteLog(object msg,bool isError = false) 
        {
            if (log == null) 
            {
                Console.WriteLine(msg.ToString());
                return;
            }

            if (IsDebugMode)
            {
                if(isError) log.Error(msg);
                else log.Debug(msg);
            }
            else 
            {
                if (isError) log.Error(msg);
                else log.Info(msg);
            }
        }
    }
}
