using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Management;

namespace ustalert_sidd
{

    internal class UsbNotification
    {
        public const int DbtDevicearrival = 0x8000; // system detected a new device        
        public const int DbtDeviceremovecomplete = 0x8004; // device is gone      
        public const int WmDevicechange = 0x0219; // device change event      
        private const int DbtDevtypDeviceinterface = 5;
        private static readonly Guid GuidDevinterfaceUSBDevice = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED"); // USB devices
        private static IntPtr notificationHandle;

        /// <summary>
        /// Registers a window to receive notifications when USB devices are plugged or unplugged.
        /// </summary>
        /// <param name="windowHandle">Handle to the window receiving notifications.</param>
        public static void RegisterUsbDeviceNotification(IntPtr windowHandle)
        {
            DevBroadcastDeviceinterface dbi = new DevBroadcastDeviceinterface
            {
                DeviceType = DbtDevtypDeviceinterface,
                Reserved = 0,
                ClassGuid = GuidDevinterfaceUSBDevice,
                Name = 0
            };

            dbi.Size = Marshal.SizeOf(dbi);
            IntPtr buffer = Marshal.AllocHGlobal(dbi.Size);
            Marshal.StructureToPtr(dbi, buffer, true);

            notificationHandle = RegisterDeviceNotification(windowHandle, buffer, 0);
        }

        /// <summary>
        /// Unregisters the window for USB device notifications
        /// </summary>
        public static void UnregisterUsbDeviceNotification()
        {
            UnregisterDeviceNotification(notificationHandle);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, int flags);

        [DllImport("user32.dll")]
        private static extern bool UnregisterDeviceNotification(IntPtr handle);

        [StructLayout(LayoutKind.Sequential)]
        private struct DevBroadcastDeviceinterface
        {
            internal int Size;
            internal int DeviceType;
            internal int Reserved;
            internal Guid ClassGuid;
            internal short Name;
        }
    }


    class main : Form
    {


        
        public const int WM_DEVICECHANGE = 0x0219; //see msdn site
        
        public static void log(string message)
        {
            message += System.Environment.NewLine;
            
            File.AppendAllText(usbService.logFile, message);
        }

        public main()
        {
            try
            {
                UsbNotification.RegisterUsbDeviceNotification(this.Handle);
                DesktopInteract();
            }
            catch (Exception e) {
                log("Error: " + e.Message);
            }

            

            
            
        }

        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            try
            {
                if (m.Msg == WM_DEVICECHANGE)
                {
                    UsbApi.DEV_BROADCAST_HDR pHdr = new UsbApi.DEV_BROADCAST_HDR();
                    pHdr = (UsbApi.DEV_BROADCAST_HDR)Marshal.PtrToStructure(m.LParam, typeof(UsbApi.DEV_BROADCAST_HDR));

                    switch ((int)m.WParam)
                    {
                        case UsbNotification.DbtDeviceremovecomplete:
                            //Console.WriteLine("Usb_DeviceRemoved");
                            log("USB REMOVED");
                            break;

                        case UsbNotification.DbtDevicearrival:

                            //log("USB ADDED" );
                            //Thread.Sleep(1000);
                            if(pHdr.dbch_DeviceType == 5)
                            {
                                log("Message Recieved: " + DateTime.Now);

                                getAndLogDetails(pHdr, m);
                            }
                               
                            break;

                    }
                }
                base.WndProc(ref m);
            }

            catch(Exception e)
            {
                //log("Error: " + e.Message.ToString());
            }

        }

        
        public void getAndLogDetails(UsbApi.DEV_BROADCAST_HDR phdr, Message m)
        {
        
            uint test = 1;
            int searchFlag = 0;
            //log(phdr.dbch_DeviceType.ToString());

            UsbApi.DEV_BROADCAST_DEVICEINTERFACE pDevInf = (UsbApi.DEV_BROADCAST_DEVICEINTERFACE)Marshal.PtrToStructure(m.LParam, typeof(UsbApi.DEV_BROADCAST_DEVICEINTERFACE));
            //log(pDevInf.dbcc_name.ToString());
            //string name = GetDeviceName(pDevInf);
            string pid = "";
            string vid = "";
            string serianNo = pDevInf.dbcc_name.Split('#')[2];
            getDetails(pDevInf.dbcc_name.ToString(), ref pid, ref vid);
            //log(pid.ToString());
            //log(vid.ToString());
            Guid DiskGUID = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");
            IntPtr h = UsbApi.SetupDiGetClassDevs(ref DiskGUID, new IntPtr(0), IntPtr.Zero, UsbApi.DIGCF_PRESENT | UsbApi.DIGCF_DEVICEINTERFACE);
            if (h != (IntPtr)UsbApi.INVALID_HANDLE_VALUE)
            {
                bool Success = true;
                uint i = 0;
               
                while (Success)
                {
                    // create a Device Interface Data structure
                    UsbApi.SP_DEVICE_INTERFACE_DATA dia = new UsbApi.SP_DEVICE_INTERFACE_DATA();
                    dia.cbSize = Marshal.SizeOf(dia);

                    // start the enumeration
                    Success = UsbApi.SetupDiEnumDeviceInterfaces(h, IntPtr.Zero, ref DiskGUID, i, ref dia);
                    if (Success)
                    {
                        // build a DevInfo Data structure
                        //Console.WriteLine("DiskGUID Agian: " + DiskGUID.ToString());
                        UsbApi.SP_DEVINFO_DATA da = new UsbApi.SP_DEVINFO_DATA();
                        da.cbSize = 28; // this becomes 32 for 64 bit systems;

                        // build a Device Interface Detail Data structure
                        UsbApi.SP_DEVICE_INTERFACE_DETAIL_DATA didd = new UsbApi.SP_DEVICE_INTERFACE_DETAIL_DATA();
                        didd.cbSize = 4 + Marshal.SystemDefaultCharSize; // trust me :)

                        int nRequiredSize = 0;
                        int nBytes = 256; //Buffer Size = 256
                        uint n_required_size = (uint)(int)nRequiredSize;    
                        uint nbytes = (uint)(int)nBytes;
                        if (UsbApi.SetupDiGetDeviceInterfaceDetail(h, ref dia, ref didd, nbytes, out n_required_size, ref da))
                        {

                            // Now we get the InstanceID of the USB level device i++;
                            IntPtr ptrInstanceBuf = Marshal.AllocHGlobal(nBytes);
                            UsbApi.CM_Get_Device_ID(da.devInst, ptrInstanceBuf, nBytes, 0);
                            string InstanceID = Marshal.PtrToStringAuto(ptrInstanceBuf);
                            //Console.WriteLine("Instance ID: " + da.devInst);

                            //Console.WriteLine("PID: {0}\nVID: {1}", pid, vid);
                            string pid_temp = "", vid_temp = "";
                            int storageFlag = 0;
                            getDetails(InstanceID.ToString(), ref pid_temp, ref vid_temp);
                            //log("Searching: " + pid_temp + "\t" + vid_temp); 
                            if (pid == pid_temp && vid == vid_temp)
                            {

                                //-----Build Device Tree and log Details ----
                                //log("-----Build Device Tree and log Details ----");
                                test = da.devInst;
                                searchFlag = 1;
                                
                                //-----------------------------------------------
                                Marshal.FreeHGlobal(ptrInstanceBuf);
                                break;
                            }
                            Marshal.FreeHGlobal(ptrInstanceBuf);

                        }
                    }
                    i++;

                }
               UsbApi.SetupDiDestroyDeviceInfoList(h);
            }
            else
                log("Invalid Handle Value");
            if (searchFlag == 1)
            {
                node device = new node(test);
                //log(da.devInst.ToString());
                deviceTree devTree = new deviceTree(device, pid, vid, serianNo);
                devTree.buildtree(ref device);
                devTree.logDetails();
            }
            else
                Console.WriteLine("Device Not Found");

        }

        public void Execute()
        {
            Form f = new main();
            f.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            //f.ShowInTaskbar = false;
            f.StartPosition = FormStartPosition.Manual;
            f.Location = new System.Drawing.Point(-2000, -2000);
            f.Size = new System.Drawing.Size(1, 1);
            Application.Run(f);

        }

        //main functions
        //Port number and hub number
        public void getPort(string location, ref string port)
        {
            try {
                string temp = location.Split('.')[0];
                string portTemp = "";
                log("temp" + temp + " " + temp.Length.ToString());
                for (int i = temp.Length - 4; i < temp.Length; i++)
                {
                    portTemp += temp[i];
                }
                log("Port Number: " + portTemp);
                port = portTemp;
            }
            catch(Exception e)
            {
                log("Error: " + e.Message);
            }
        }

        //Gets the PID and VID from the device path
        public static bool getDetails(String path, ref string pid, ref string vid)
        {
            // PATH=  \\?\USB#VID_062A&PID_4101#<RANDON NUMBERS>
            for (int i = 2; i < path.Length; i++)
            {
                if (path[i - 2] == 'P' && path[i - 1] == 'I' && path[i] == 'D')
                {
                    i += 2;
                    pid = "";
                    pid += path[i];
                    i++;
                    pid += path[i];
                    i++;
                    pid += path[i];
                    i++;
                    pid += path[i];
                    i++;

                }
                if (path[i - 2] == 'V' && path[i - 1] == 'I' && path[i] == 'D')
                {
                    i += 2;
                    vid = "";
                    vid += path[i];
                    i++;
                    vid += path[i];
                    i++;
                    vid += path[i];
                    i++;
                    vid += path[i];
                    i++;

                }
                else
                    i++;
            }
            return true;
        }


        private void DesktopInteract()
        {
            ManagementObject wmiService = null;
            ManagementBaseObject InParam = null;
            string ServiceName = "usbalertSidd";
            try
            {
                wmiService = new ManagementObject(string.Format("Win32_Service.Name='{0}'",
                                ServiceName));
                InParam = wmiService.GetMethodParameters("Change");
                InParam["DesktopInteract"] = true;
                wmiService.InvokeMethod("Change", InParam, null);
            }
            finally
            {
                if (InParam != null)
                    InParam.Dispose();
                if (wmiService != null)
                    wmiService.Dispose();
            }
        }

        //Checks status and then ejects the provided device instance ID
        public static bool checkEject(uint devInst)
        {

            int CR_SUCCESS = 0x00000000;
            int DN_REMOVABLE = 0x00004000;
            UInt32 status;
            UInt32 problem;

            if (CR_SUCCESS == UsbApi.CM_Get_DevNode_Status(out status, out problem, devInst, 0) && (DN_REMOVABLE & status) > 0)
            {
                UsbApi.PNP_VETO_TYPE pnp_veto_type;
                System.Text.StringBuilder sb = new System.Text.StringBuilder(255);
            
                bool success = (CR_SUCCESS == UsbApi.CM_Request_Device_Eject((IntPtr)devInst, out pnp_veto_type, sb, sb.Capacity, 0));
                if (success)
                {
                    //log("Device Ejected");
                }
                ///else log("Eject Fail");
                log("Ejected @ " + DateTime.Now);
                return success;
                //Console.WriteLine("EJECT: " + success.ToString());
            }
            else
            {
                //log("Could not Eject");
                log("Ejected Failed @ " + DateTime.Now);
                return false;
                //Console.WriteLine("Status False");
            }

        }



        //Constants
        /*
        const int DIGCF_DEFAULT = 0x1;
        const int DIGCF_PRESENT = 0x2;
        const int DIGCF_ALLCLASSES = 0x4;
        const int DIGCF_PROFILE = 0x8;
        const int DIGCF_DEVICEINTERFACE = 0x10;

        static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);


        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_HDR
        {
            public uint dbch_Size;
            public uint dbch_DeviceType;
            public uint dbch_Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_VOLUME
        {
            public int dbcv_size;
            public int dbcv_devicetype;
            public int dbcv_reserved;
            public int dbcv_unitmask;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string dbcc_name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public Int32 cbSize;
            public Guid interfaceClassGuid;
            public Int32 flags;
            private UIntPtr reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid classGuid;
            public uint devInst;
            public IntPtr reserved;
        }

        public enum PNP_VETO_TYPE : int
        {
            PNP_VetoTypeUnknown = 0,
            PNP_VetoLegacyDevice = 1,
            PNP_VetoPendingClose = 2,
            PNP_VetoWindowsApp = 3,
            PNP_VetoWindowsService = 4,
            PNP_VetoOutstandingOpen = 5,
            PNP_VetoDevice = 6,
            PNP_VetoDriver = 7,
            PNP_VetoIllegalDeviceRequest = 8,
            PNP_VetoInsufficientPower = 9,
            PNP_VetoNonDisableable = 10,
            PNP_VetoLegacyDriver = 11,
            PNP_VetoInsufficientRights = 12
        }

        //Functions Definitions

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SetupDiGetClassDevs(
                                             ref Guid ClassGuid,
                                             IntPtr Enumerator,
                                             IntPtr hwndParent,
                                             uint Flags
                                            );

        [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean SetupDiGetDeviceInterfaceDetail(
           IntPtr hDevInfo,
           ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
           ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
           UInt32 deviceInterfaceDetailDataSize,
           out UInt32 requiredSize,
           ref SP_DEVINFO_DATA deviceInfoData
        );

        [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean SetupDiEnumDeviceInterfaces(
           IntPtr hDevInfo,
           IntPtr devInfo,
           ref Guid interfaceClassGuid,
           UInt32 memberIndex,
           ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData
        );

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        public static extern int CM_Get_Device_ID(
           UInt32 dnDevInst,
           IntPtr buffer,
           int bufferLen,
           int flags
        );

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        static extern int CM_Request_Device_Eject(
            IntPtr devinst, 
            out PNP_VETO_TYPE pVetoType, 
            System.Text.StringBuilder pszVetoName, 
            int ulNameLength, 
            int ulFlags
        );

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern int CM_Get_DevNode_Status(
            out UInt32 status, 
            out UInt32 probNum, 
            UInt32 devInst, 
            int flags
        );

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern int CM_Get_Child(
            out UInt32 pdnDevInst,
            UInt32 dnDevInst,
            int ulFlags
        );

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern int CM_Get_Sibling(
            out UInt32 pdnDevInst,
            UInt32 dnDevInst,
            int ulFlags
        );

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern int CM_Get_DevNode_Registry_Property(
            uint deviceInstance,
            uint property,
            out Microsoft.Win32.RegistryValueKind pulRegDataType,
            IntPtr buffer,
            ref uint length,
            uint flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr deviceInfoSet,
            ref SP_DEVINFO_DATA deviceInfoData,
            uint property,
            out UInt32 propertyRegDataType,
            byte[] propertyBuffer, // the difference between this signature and the one above.
            uint propertyBufferSize,
            out UInt32 requiredSize
        );

        */

    }
}
