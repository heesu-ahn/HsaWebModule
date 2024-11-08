using System;

namespace HsaWebModule
{
    public abstract class TestTcpServerBase : IDisposable
    {
        public LifeCycleManager manager;
        public TestTcpServerBase() 
        {
            manager = new LifeCycleManager(this);
        }
        public void Dispose()
        {
            Program.WriteLog($"{manager.ClassName} Class Dispose.");
        }
    }
}
