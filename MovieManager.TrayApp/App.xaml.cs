using Hardcodet.Wpf.TaskbarNotification;
using MovieManager.Endpoint;
using System;
using System.Diagnostics;
using System.Management;
using System.Windows;

namespace MovieManager.TrayApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon notifyIcon;
        public Process WebAppProcess;
        public Process HttpServerProcess;

        public void Test() { }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            Program.Run(null);
            ExecuteCommands();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
            KillProcessAndChildrens(WebAppProcess.Id);
            KillProcessAndChildrens(HttpServerProcess.Id);
            base.OnExit(e);
        }

        private void ExecuteCommands()
        {
            var webAppProcessInfo = new ProcessStartInfo("cmd.exe", "/K " + @"serve C:\Projects\MovieManager\MovieManager.Web\build");
            var httpServerProcessInfo = new ProcessStartInfo("cmd.exe", "/K " + "http-server E:/");
            webAppProcessInfo.CreateNoWindow = true;
            httpServerProcessInfo.CreateNoWindow = true;
            WebAppProcess = Process.Start(webAppProcessInfo);
            HttpServerProcess = Process.Start(httpServerProcessInfo);
        }

        private void KillProcessAndChildrens(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }

            if (processCollection != null)
            {
                foreach (ManagementObject mo in processCollection)
                {
                    KillProcessAndChildrens(Convert.ToInt32(mo["ProcessID"])); 
                }
            }
        }
    }
}
