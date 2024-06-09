using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System.Text;
using System.Windows.Forms;

namespace HsaWebModule
{
    public class Log4netManager
    {
        public ILog log;
        public Log4netManager(string fileWatcherPath, string logLevel = "")
        {
            if (!string.IsNullOrEmpty(fileWatcherPath))
            {
                Hierarchy hierarchy = new Hierarchy();
                RollingFileAppender rollingAppender = new RollingFileAppender();
                PatternLayout layout = new PatternLayout();

                hierarchy = (Hierarchy)LogManager.GetRepository();
                hierarchy.Configured = true;

                rollingAppender.Name = Application.ProductName;
                rollingAppender.File = fileWatcherPath;
                rollingAppender.Encoding = Encoding.UTF8;
                rollingAppender.AppendToFile = true;

                rollingAppender.DatePattern = "yyyyMMdd'_" + Application.ProductName + ".log'";
                rollingAppender.RollingStyle = RollingFileAppender.RollingMode.Composite;
                rollingAppender.LockingModel = new RollingFileAppender.MinimalLock();
                rollingAppender.StaticLogFileName = false;
                rollingAppender.MaxSizeRollBackups = 100;
                rollingAppender.MaximumFileSize = "10MB";
                layout = new PatternLayout("%date %level %logger - %message%newline");
                layout.ActivateOptions();

                rollingAppender.Layout = layout;

                ConsoleAppender consoleAppender = new ConsoleAppender() 
                {
                    Name = "ConsoleAppender",
                    Layout = layout
                };


                hierarchy.Root.AddAppender(rollingAppender);
                hierarchy.Root.AddAppender(consoleAppender);
                hierarchy.Configured = true;
                rollingAppender.ActivateOptions();

                if (logLevel.Equals("INFO"))
                {
                    hierarchy.Root.Level = log4net.Core.Level.Info;
                }
                else if (logLevel.Equals("ERROR"))
                {
                    hierarchy.Root.Level = log4net.Core.Level.Error;
                }
                else if (logLevel.Equals("DEBUG"))
                {
                    hierarchy.Root.Level = log4net.Core.Level.Debug;
                }
                else
                {
                    hierarchy.Root.Level = log4net.Core.Level.All;
                }
                log = LogManager.GetLogger(Application.ProductName);
            }
        }
        public ILog SetLogger()
        {
            return log;
        }
    }
}