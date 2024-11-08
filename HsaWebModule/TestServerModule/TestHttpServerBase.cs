using System;

namespace HsaWebModule
{
    public abstract class TestHttpServerBase : IDisposable
    {
        public LifeCycleManager manager;
        public TestHttpServerBase() 
        {
            manager = new LifeCycleManager(this);
        }
        public void Dispose()
        {
            Program.WriteLog($"{manager.className} Class Dispose.");
        }
    }
}
