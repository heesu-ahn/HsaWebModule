using System.Reflection;

namespace HsaWebModule
{
    public class LifeCycleManager
    {
        public object ParentObject;
        public string className = "";
        public LifeCycleManager(object Class)
        {
            this.ParentObject = Class;
        }
        public string ClassName 
        {
            get => className;
            set 
            {
                if (className == "" && !string.IsNullOrEmpty(value)) 
                {
                    Program.log.Debug($"{value}:ProcessStart.");
                    className = value;
                }
            }
        }

        public bool processEnd = false;
        public bool ProcessEnd
        {
            get => processEnd;
            set
            {
                if (value != processEnd)
                {
                    processEnd = value;
                    if (processEnd)
                    {
                        if (ParentObject != null && !string.IsNullOrEmpty(className))
                        {
                            if (Program.TestServerType["Tcp"].Equals(className))
                            {
                                MethodInfo tcpMethod = typeof(TestTcpServer).GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance);
                                tcpMethod.Invoke((TestTcpServer)ParentObject, null);
                                ParentObject = ((TestTcpServer)ParentObject).CreateNewObject();
                            }
                            else if (Program.TestServerType["Http"].Equals(className))
                            {
                                MethodInfo httpMethod = typeof(TestHttpServer).GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance);
                                httpMethod.Invoke((TestHttpServer)ParentObject, null);
                                ParentObject = ((TestHttpServer)ParentObject).CreateNewObject();
                            }
                        }
                    }
                }
            }
        }
    }
}
