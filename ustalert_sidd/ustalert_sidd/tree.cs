using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ustalert_sidd
{
    public class node
    {
        public UInt32 deviceInstance;
        public string deviceClass;
        List<node> children;

        public node(UInt32 instance)
        {
            deviceInstance = instance;
            children = new List<node>();
            deviceClass = setDeviceClass();
        }

        public string setDeviceClass()
        {
            string classFinal = "";

            Microsoft.Win32.RegistryValueKind kind;
            uint length = 0;

            UsbApi.CM_Get_DevNode_Registry_Property(deviceInstance, 0x00000001, out kind, IntPtr.Zero, ref length, 0);
            IntPtr buffer1 = Marshal.AllocHGlobal((int)length);

            if (UsbApi.CM_Get_DevNode_Registry_Property(deviceInstance, 0x00000001, out kind, buffer1, ref length, 0) == 0)
            {
                string driver_detail = Marshal.PtrToStringAnsi(buffer1);
                //Console.WriteLine("\tDRIVER NAME: {0}", driver_name.ToString());
                classFinal += driver_detail + " ";

            }
            else
            {
                main.log("could not get Class");
                //return "";
            }

            UsbApi.CM_Get_DevNode_Registry_Property( deviceInstance, 0x00000008, out kind, IntPtr.Zero, ref length, 0);
            IntPtr buffer = Marshal.AllocHGlobal((int)length);

            if (UsbApi.CM_Get_DevNode_Registry_Property(deviceInstance, 0x00000008, out kind, buffer, ref length, 0) == 0)
            {
                string driver_detail = Marshal.PtrToStringAnsi(buffer);
                //Console.WriteLine("\tDRIVER NAME: {0}", driver_name.ToString());
                classFinal += driver_detail;    
                //return driver_detail;

            }
            else
            {
                //main.log("could not get Class");
                
            }
            return classFinal;
        }

        public int addChild(node child)
        {
            try
            {
                this.children.Add(child);
                return 0;
            }
            catch
            {
                return 1;
            }

        }
    }


    public class deviceTree
    {
        node root;
        public string name;
        public string pid;
        public string vid;
        public string serialNo;
        public string deviceClassification; //The Final Classification
        public string deviceClasses; //classes of all the children in the USB device
        public int storageFlag;
        string device = "0000000000000000";

        public deviceTree(node r, string pid, string vid, string serialNo)
        {
            this.root = r;
            this.pid = pid;
            this.vid = vid;
            this.serialNo = serialNo;
            deviceClasses = "";
            deviceClasses += root.deviceClass;
            deviceClassification = "";
        }

        public void buildtree(ref node root)
        {
            UInt32 child1;
            if (UsbApi.CM_Get_Child(out child1, root.deviceInstance, 0) == 0)
            {
                node childNode = new node(child1);
                root.addChild(childNode);
                deviceClasses += childNode.deviceClass + " 2 ";
                this.buildtree(ref childNode);
                //main.log("child added: " + childNode.deviceClass);
                UInt32 child;
                while (UsbApi.CM_Get_Sibling(out child, child1, 0) == 0)
                {
                    node childSibling = new node(child);
                    root.addChild(childSibling);
                    this.deviceClasses += childSibling.deviceClass + " 1 ";
                    buildtree(ref childSibling);
                    child1 = child;
                    child = 0;
                    //main.log("child added: " + childSibling.deviceClass);
                }

            }
            //free(child1);


        }


        public bool has(string property)
        {
            //main.log("Property: " + property);
            string tempProp = property.ToUpper();
            this.deviceClasses = this.deviceClasses.ToUpper();
            if (this.deviceClasses.Contains(tempProp))
            {

                //main.log("true");
                return true;
            }
            else
            {
                return false;
            }
        }

        public void replace(int index)
        {
            StringBuilder temp = new StringBuilder(this.device);
            temp[index] = '1';
            this.device = temp.ToString();
            

        }

        public int build()
        {
            //main.log("this is the final: " + this.deviceClasses);
            int size = 16;
            if (has("Volume"))
            {
                replace(size - 1);
            }
            if (has("Biometric"))
            {
                replace(size - 2);
            }
            if (has("Bluetooth"))
            {
                replace(size - 3);
            }
            if (has("CDROM"))
            {
                replace(size - 4);
            }
            if (has("Disk Drive") || has("DiskDrive"))
                replace(size - 5);
            if (has("Modem"))
                replace(size - 6);
            if (has("HIDClass"))
                replace(size - 7);
            if (has("Image"))
                replace(size - 8);
            if (has("Keyboard"))
                replace(size - 9);
            if (has("MTD"))
                replace(size - 10);
            if (has("Mouse"))
                replace(size - 11);
            if (has("Net") || has("Virtual Adapter") || has("801") || has("Network"))
                replace(size - 12);
            if (has("Printer"))
                replace(size - 13);
            if (has("Sensor"))
                replace(size - 14);
            if (has("USBDevice") || has("Usb Device")) 
                replace(size - 15);
            if (has("WPD"))
                replace(size - 16);

            return Convert.ToInt32(this.device,2);
        }

        public void classify()
        {
            string classes = this.deviceClasses;
            classes = classes.ToUpper();
            
            int dev = build();
            //main.log("BUILD : " + this.storageFlag + " " + dev.ToString());
            if (dev == 16 || (has("USB MASS STORAGE") && has("Disk Drive")))
                this.deviceClassification = "Pendrive";
            else if (dev == 320 || dev == 256)
                this.deviceClassification = "Keyboard";
            else if (dev == 1344 || dev == 1088)
                this.deviceClassification = "Mouse";
            else if (has("print") || has("Dot4Print") || has("Printer") || has("Office-Jet") || has("officeJet"))
                this.deviceClassification = "Printer";
            else if (has("BlueTooth") || dev == 4)
                this.deviceClassification = "Bluetooth Device";
            else if (has("Net") || dev == 2048)
                this.deviceClassification = "WireLess Adapter";
            else if (has("USB Composite") && (has("Mass Storage") || has("DiskDrive") || has("Disk Drive")))
            {
                this.deviceClassification = "Phone";
                this.storageFlag = 1;
            }
            else if (has("USB Composite") && (has("Modem") || has("Port")))
            {
                this.deviceClassification = "Phone";
                this.storageFlag = 1;
            }
                
            else if (has("USB Composite") && (has("WinUsb")))
            {
                this.deviceClassification = "Windows Phone";
                this.storageFlag = 1;
            }
              
            else if (dev == 8)
            {
                this.deviceClassification = "CRDOM";
                this.storageFlag = 1;
            }
                

            else
            {
                this.storageFlag = 1;
                this.deviceClassification = "SmartPhone";
            }
            if (classes.Contains("USBSTOR") || classes.Contains("DISKDRIVE") || classes.Contains("MASS STORAGE") || classes.Contains("STORAGE") || classes.Contains("WPD"))
                this.storageFlag = 1;
            if (classes.Contains("INPUT DEVICE") || classes.Contains("HIDCLASS") || classes.Contains("KEYBOARD") || classes.Contains("MOUSE"))
            {
                this.storageFlag = 0;
                this.deviceClassification = "Input Device";
            }
                

            }


        public void logDetails()
        {
            classify();
            string finalLog = "" + this.pid + ", " + this.vid + ", " + this.serialNo + ", " + this.deviceClassification + ", " + this.storageFlag + ", " + DateTime.Now.ToString() ;
           
            //Thread.Sleep(1000);

            if(this.storageFlag == 1)
            {
                bool eject = main.checkEject(root.deviceInstance);
                if (eject == true)
                    finalLog += ", " + "Ejected";
                else
                {
                    finalLog += ", " + "Eject Fail";
                }
               
            }
            else
            {
                finalLog += ", " + "Device Allowed";
            }
            try
            {
                TurnOnScreenSaver();
            }
            catch(Exception e)
            {
                main.log("Error: " + e.Message);
            }
            
            main.log(finalLog);
            string fileName = AppDomain.CurrentDomain.BaseDirectory;
            
            main.log(fileName);
            if (storageFlag == 1) {
                main.log("Process II");
                try {
                    //C: \Users\Padhi PC\Documents\Visual Studio 2015\Projects\ustalert_sidd\lockScreen\bin\Release
                    System.Diagnostics.Process.Start(@"//C: \Users\Padhi PC\Documents\Visual Studio 2015\Projects\ustalert_sidd\lockScreen\bin\Release\lockScreen.exe");
                    Process test = new Process();
                    //string file = "";
                    
                    test.StartInfo.FileName = "notepad.exe";
                    test.EnableRaisingEvents = true;
                    test.Start();
                    test.WaitForExit();
                }
                catch(Exception e)
                {
                    main.log("Error: " + e.Message);
                }
            }

            main.log("Lock" );
            
        }
        
        private const int WmSyscommand = 0x0112;
        private const int ScMonitorpower = 0xF170;
        private const int HwndBroadcast = 0xFFFF;
        private const int ShutOffDisplay = 2;
        [DllImport("user32.dll")]
        private static extern void LockWorkStation();
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg,
                      IntPtr wParam, IntPtr lParam);
        private static void TurnOffDisplay()
        {
            PostMessage((IntPtr)HwndBroadcast, (uint)WmSyscommand,
                    (IntPtr)ScMonitorpower, (IntPtr)ShutOffDisplay);
        }
        [DllImport("User32.dll")]
        public static extern int SendMessage
        (IntPtr hWnd,
        uint Msg,
        uint wParam,
        uint lParam);
        public const uint WM_SYSCOMMAND = 0x112;
        public const uint SC_SCREENSAVE = 0xF140;
        public enum SpecialHandles
        {
            HWND_DESKTOP = 0x0,
            HWND_BROADCAST = 0xFFFF
        }
        public static void TurnOnScreenSaver()
        {
            main.log("ScreenSacer-> " + 
                SendMessage(
                new IntPtr((int)SpecialHandles.HWND_BROADCAST),
                WM_SYSCOMMAND,
                SC_SCREENSAVE,
                0).ToString());
        }
    }

}
/*
                                //-----Build Device Tree and log Details ----
                                log("-----Build Device Tree and log Details ----");
                                node device = new node(da.devInst);
                                deviceTree devTree = new deviceTree(device, pid, vid, " ");
                                devTree.buildtree(ref device);
                                devTree.logDetails();
                                log("-------------------------------------------");
                                //-------------------------------------------


*/



/*

//Checking for the Keyboard!!----------------

                            const int BUFFER_SIZE = 256;
                            uint length = 0;
                            Microsoft.Win32.RegistryValueKind kind;
                            byte[] buf = new byte[BUFFER_SIZE];

                            UInt32 child1, child11, child12, child2, child21;

                            //got first child
                            CM_Get_Child(out child1, da.devInst, 0);
                            log("CHILD: " + child1.ToString());
                            CM_Get_DevNode_Registry_Property(child1, 0x00000001, out kind, IntPtr.Zero, ref length, 0);
                            IntPtr buffer = Marshal.AllocHGlobal((int)length);

                            if (CM_Get_DevNode_Registry_Property(child1, 0x00000001, out kind, buffer, ref length, 0) == 0)
                            {
                                string driver_name = Marshal.PtrToStringAnsi(buffer);
                                //Console.WriteLine("\tDRIVER NAME: {0}", driver_name.ToString());
                                log("Child1 -> " + driver_name.ToString());

                            }
                            else
                                log("CM FAIL");

                            //got child child11
                            CM_Get_Child(out child11, child1, 0);
                            log("\tChild:" + child11.ToString());
                            int error = CM_Get_DevNode_Registry_Property(child11, 0x00000008, out kind, IntPtr.Zero, ref length, 0);
                            IntPtr buffer11 = Marshal.AllocHGlobal((int)length);
                            if (CM_Get_DevNode_Registry_Property(child11, 0x00000008, out kind, buffer11, ref length, 0) == 0)
                            {
                                string driver_name = Marshal.PtrToStringAnsi(buffer11);
                                log("\tChild11 -> " + driver_name.ToString());
                            }
                            else
                                log("CM_Fail " + error.ToString());


                            //got second child
                            CM_Get_Sibling(out child2, child1, 0);
                            log("Child 2: " + child2.ToString());
                            error =CM_Get_DevNode_Registry_Property(child2, 0x00000008, out kind, IntPtr.Zero, ref length, 0);
                            IntPtr buffer2 = Marshal.AllocHGlobal((int)length);
                            if (CM_Get_DevNode_Registry_Property(child2, 0x00000008, out kind, buffer2, ref length, 0) == 0)
                            {
                                string driver_name = Marshal.PtrToStringAnsi(buffer2);
                                log("Child2 -> " + driver_name.ToString());
                            }
                            else
                                log("CM_FAIL " + error);


                            //got child21
                            CM_Get_Child(out child21, child2,0);
                            log("\tCHILD 21: " + child2.ToString());
                            error = CM_Get_DevNode_Registry_Property(child21, 0x00000008, out kind, IntPtr.Zero, ref length, 0);
                            IntPtr buffer21 = Marshal.AllocHGlobal((int)length);
                            if (CM_Get_DevNode_Registry_Property(child21, 0x00000008, out kind, buffer21, ref length, 0) == 0)
                            {
                                string driver_detail = Marshal.PtrToStringAnsi(buffer21);
                                log("\tChild21 -> " + driver_detail.ToString());
                            }
                            else
                                log("CM_FAIL " + error);
                            //-------------------------------------------


*/
