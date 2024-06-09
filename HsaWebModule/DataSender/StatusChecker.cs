using System.Threading;
using System;

namespace HsaWebModule
{
    public class StatusChecker
    {
        public int invokeCount;
        public int maxCount;
        public bool checkEnd = false;
        private int failCount = 0;

        public StatusChecker(int count)
        {
            try
            {
                invokeCount = 0;
                maxCount = count;
            }
            catch (Exception ex)
            {
                Program.log.Debug(ex.Message);
            }
        }

        // This method is called by the timer delegate.
        public void CheckStatus(object stateInfo)
        {
            try
            {
                AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;
                Console.WriteLine(string.Format("{0} Checking status {1,2}.", DateTime.Now.ToString("yyyy-MM-DD HHh:mm:ss.fff"), (++invokeCount).ToString()));
                if (failCount > 2) 
                {
                    autoEvent.Set();
                    checkEnd = true;
                }
                if (invokeCount == 1) failCount ++;
                if (invokeCount == maxCount)
                {
                    // Reset the counter and signal the waiting thread.
                    invokeCount = 0;
                    autoEvent.Set();
                    checkEnd = true;
                }
            }
            catch (Exception ex)
            {
                Program.log.Debug(ex.Message);
            }
        }
    }
}