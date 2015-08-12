using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.IO;
using System.Threading;
using System.Text;
using System.Threading.Tasks;

namespace WindowsService1
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }


        public void onDebug()
        {
            OnStart(null);
            Execute();
        }

        public void Execute()
        {
            
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "onStart.txt", DateTime.Now.ToString());
            Thread.Sleep(1000);
            Execute();
            
        }

        protected override void OnStart(string[] args)
        {
            FileStream file = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "onStart.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            file.Close();
            
        }

        protected override void OnStop()
        {
            FileStream file = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "onStop.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            file.Close();

        }
    }
}
