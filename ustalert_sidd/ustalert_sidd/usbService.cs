using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace ustalert_sidd
{
    
    public partial class usbService : ServiceBase
    {
        public static string logFile = AppDomain.CurrentDomain.BaseDirectory + "Logs.txt";


        public usbService()
        {
            InitializeComponent();
        }

        public void onDebug()
        {
            OnStart(null);
        }

        // Class Initializarion
        public void mainFunc()
        {
#if DEBUG
            //Debugger.Launch();
            main test = new main();
            OnStart(null);


#else
            string baseDirec = AppDomain.CurrentDomain.BaseDirectory;
            File.AppendAllText(logFile, "SERVICE STARTED @ " + DateTime.Now.ToString() + System.Environment.NewLine);

            main test = new main();
            ThreadStart ex = new ThreadStart(test.Execute);
            Thread testChild = new Thread(ex);
            testChild.Start();
        
#endif
        }
        protected override void OnStart(string[] args)
        {
            mainFunc();
        }

        protected override void OnStop()
        {
            File.AppendAllText(logFile, "SERVICE STOPPED @ " + DateTime.Now.ToString() + System.Environment.NewLine);
        }

        private void eventLog1_EntryWritten(object sender, EntryWrittenEventArgs e)
        {

        }
        /*
        static void SetRecoveryOptions(string serviceName)
        {
            int exitCode;
            using (var process = new Process())
            {
                var startInfo = process.StartInfo;
                startInfo.FileName = "sc";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                // tell Windows that the service should restart if it fails
                startInfo.Arguments = string.Format("failure \"{0}\" reset= 0 actions= restart/60000", serviceName);

                process.Start();
                process.WaitForExit();

                exitCode = process.ExitCode;
            }

            if (exitCode != 0)
                throw new InvalidOperationException();
        }*/
    }
}
