﻿using System;

namespace HsaWebModule
{
    public abstract class TestWebSocketServerBase : IDisposable
    {
        public LifeCycleManager manager;
        public TestWebSocketServerBase()
        {
            manager = new LifeCycleManager(this);
        }
        public void Dispose()
        {
            Program.log.Debug($"{manager.className} Class Dispose.");
        }
    }
}
