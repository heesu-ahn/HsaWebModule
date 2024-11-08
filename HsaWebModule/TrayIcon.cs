using HsaWebModule.Properties;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HsaWebModule.Screen
{
    public class TrayIcon
    {
        public NotifyIcon notifyIcon;
        private List<KeyValuePair<string,EventHandler>> eventHandlers = new List<KeyValuePair<string, EventHandler>>();  

        public TrayIcon() 
        {
            notifyIcon = new NotifyIcon();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            eventHandlers.Add(new KeyValuePair<string, EventHandler>(string.Format("{0}_Settings", Application.ProductName), new EventHandler(Settings)));
            eventHandlers.Add(new KeyValuePair<string, EventHandler>(string.Format("{0}_Exit", Application.ProductName), new EventHandler(CloseApp)));
            
            AddItems(notifyIcon.ContextMenuStrip);
            notifyIcon.Text = Application.ProductName;
            notifyIcon.Icon = Resources.alpha_h_circle_icon_136971;
            notifyIcon.Visible = true;
        }

        private void Settings(object sender, EventArgs e)
        {

        }
        private void CloseApp(object sender, EventArgs e)
        {
            Program.WriteLog("프로그램 종료");
            Program.trayIcon.notifyIcon.Dispose();
            Application.Exit();
        }

        private void AddItems(ContextMenuStrip contextMenuStrip)
        {
            foreach (var item in eventHandlers) 
            {
                contextMenuStrip.Items.Add(item.Key, null, item.Value);
            }
        }
    }
}
