namespace HsaWebModule
{
    using ProgramUtil;
    using ProtocolModule;
    using Screen;
    using log4net;
    using System;

    public static class HsaWebModuleProperty
    {
        public static MainProperty mainProperty = new MainProperty();
        public static HttpServerProperty httpServerProperty = new HttpServerProperty(null);
        public static TcpServerProperty tcpServerProperty = new TcpServerProperty();
        public static WebSocketServerProperty webSocketServerProperty = new WebSocketServerProperty(null,"",null);
    }
    public class MainProperty 
    {
        public ILog log
        {
            get
            {
                return Program.log;
            }
            set
            {
                Program.log = value;
            }
        }
        public string programPath 
        {
            get
            {
                return Program.programPath;
            }
            set
            {
                Program.programPath = value;
            }
        }
        public string propertyConfigName = "HsaWebModule.Property";
        public System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<string, string>> Properties 
        { 
            get 
            { 
                return Program.Properties; 
            } 
            set 
            {
                Program.Properties = value;
            } 
        }
        public ProgramProperties keyCodeTable 
        {
            get
            {
                return Program.keyCodeTable;
            }
            set
            {
                Program.keyCodeTable = value;
            }
        }
        public string log4jLevel
        {
            get
            {
                return Program.log4jLevel;
            }
            set
            {
                Program.log4jLevel = value;
            }
        }
        public bool wsUseSSL
        {
            get
            {
                return Program.wsUseSSL;
            }
            set
            {
                Program.wsUseSSL = value;
            }
        }
        public string certFilePath
        {
            get
            {
                return Program.certFilePath;
            }
            set
            {
                Program.certFilePath = value;
            }
        }
        public int wsPort
        {
            get
            {
                return Program.wsPort;
            }
            set
            {
                Program.wsPort = value;
                if (HsaWebModuleProperty.webSocketServerProperty != null) 
                {
                    HsaWebModuleProperty.webSocketServerProperty.Port = value;
                }
            }
        }
        public int httpPort
        {
            get
            {
                return Program.httpPort;
            }
            set
            {
                Program.httpPort = value;
            }
        }
        public int tcpPort
        {
            get
            {
                return Program.tcpPort;
            }
            set
            {
                Program.tcpPort = value;
            }
        }
        public WebSocketService webSocketService 
        {
            get
            {
                return Program.webSocketService;
            }
            set
            {
                Program.webSocketService = value;
            }
        }
        public UserDataModule userData 
        {
            get 
            {
                return Program.userData;
            }
            set 
            {
                Program.userData = value;
            }
        }
        public string issuer = string.Empty;
        public string audience = "http://127.0.0.1:8080";
        public TrayIcon trayIcon
        {
            get
            {
                return Program.trayIcon;
            }
            set
            {
                Program.trayIcon = value;
            }
        }
        public string defaultUserName = "hsaWebModule";
        public System.Collections.Generic.Dictionary<string, string> TestServerType
        {
            get
            {
                return Program.TestServerType;
            }
            set
            {
                Program.TestServerType = value;
            }
        }
        public EncryptModule encryptModule
        {
            get
            {
                return Program.encryptModule;
            }
            set
            {
                Program.encryptModule = value;
            }
        }
        public MainProperty() 
        {
            string path = System.Windows.Forms.Application.StartupPath;
            programPath = new System.IO.DirectoryInfo(path).Parent.Parent.FullName;
            Properties = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<string, string>>();
            TestServerType = new System.Collections.Generic.Dictionary<string, string>();
            wsUseSSL = false;
            log4jLevel = string.Empty;
            certFilePath = string.Empty;
            wsPort = 0;
            httpPort = 0;
            tcpPort = 0;
        }
    }

    public class HttpServerProperty
    {
        public TestHttpListener httpListener;
        public string address 
        {
            get
            {
                if (httpListener.parent != null) return httpListener.parent.address;
                else return address;
            }
            set
            {
                if(httpListener.parent != null) httpListener.parent.address = value;
            }
        }
        private int port;
        public int Port
        {
            get
            {
                if (httpListener != null && httpListener.parent != null) return httpListener.parent.port;
                else return port;
            }
            set
            {
                if (httpListener != null && httpListener.parent != null) httpListener.parent.port = value;
            }
        }

        public string path
        {
            get
            {
                return httpListener.path;
            }
            set
            {
                httpListener.path = value;
            }
        }
        public string serviceName
        {
            get
            {
                return httpListener.serviceName;
            }
            set
            {
                httpListener.serviceName = value;
            }
        }
        public string sessionId
        {
            get
            {
                return httpListener.sessionId;
            }
            set
            {
                httpListener.sessionId = value;
            }
        }
        public bool wsServiceEnd
        {
            get
            {
                return httpListener.wsServiceEnd;
            }
            set
            {
                httpListener.wsServiceEnd = value;
            }
        }

        public HttpServerProperty(TestHttpListener _httpListener)
        {
            if (_httpListener == null)
            {
                return;
            }
            else 
            {
                httpListener = _httpListener;
                address = "http://127.0.0.1";
                port = 5000;
                path = "HsaWebModule";
                serviceName = "Service.do";
                sessionId = string.Empty;
                wsServiceEnd = false;
            }
        }
    }
    public class TcpServerProperty
    {
        public TcpServerProperty()
        {

        }
    }
    public class WebSocketServerProperty
    {
        private TestWebSocketServer webSocketServer;
        public string address = "127.0.0.1";
        private int port;
        public int Port 
        {
            get {
                return port;
            }
            set 
            {
                port = value;
            }
        }

        public string serviseName = "TestWebSocketServer";
        public string pasword = "12345";

        public TestHttpListener parentObj 
        {
            get 
            {
                return webSocketServer.parentObj;
            }
            set 
            {
                webSocketServer.parentObj = value;
            }
        }
        public WebSocketServerProperty(TestWebSocketServer _wssv, string sessionId, TestHttpListener parent)
        {
            if (_wssv == null)
            {
                return;
            }
            else 
            {
                sessionId = string.Empty;
                webSocketServer = _wssv;
            }
        }
    }
}
