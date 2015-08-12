using System.Windows.Forms;

namespace wndprocTest
{
    class serviceManager
    {
        public void StartService()
        {
            Form f = new Program();
            f.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            //f.ShowInTaskbar = false;
            f.StartPosition = FormStartPosition.Manual;
            f.Location = new System.Drawing.Point(-2000, -2000);
            f.Size = new System.Drawing.Size(1, 1);
            Application.Run(f);
            MessageBox.Show("Service Started");
        }

        public void StopService()
        {
            MessageBox.Show("Service Stopped");
        }
    }
}
