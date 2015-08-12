using System.Runtime.InteropServices;
using System.Management;
using System.Windows.Forms;
using System;
using Topshelf;
using Microsoft.Win32;
using System.Threading;
using System.Timers;

namespace wndprocTest
{

    internal static class UsbNotification
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
   
    class Program : Form
    {
        const int WM_DEVICECHANGE = 0x0219; //see msdn site
        const int DBT_DEVICEARRIVAL = 0x8000;
        const int DBT_DEVICEREMOVALCOMPLETE = 0x8004;
        const int DBT_DEVTYPVOLUME = 0x00000002;
        const int CM_DRP_DRIVER = 0x0000000A; // taken from cfgmgr32.h
        const int CR_SUCCESS = 0x00000000;
        private const int WM_ACTIVATEAPP = 0x001C;
        private static IntPtr notificationHandle;
        private static readonly Guid GuidDevinterfaceUSBDevice = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED"); // USB devices
        private bool appActive = true;
        const int DIGCF_DEFAULT = 0x1;
        const int DIGCF_PRESENT = 0x2;
        const int DIGCF_ALLCLASSES = 0x4;
        const int DIGCF_PROFILE = 0x8;
        const int DIGCF_DEVICEINTERFACE = 0x10;
        static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        Guid GUID_DEVINTERFACE_DISK = new Guid(0x53f56307, 0xb6bf, 0x11d0, 0x94, 0xf2, 0x00, 0xa0, 0xc9, 0x1e, 0xfb, 0x8b);

        searchData sd = new searchData();

        [STAThread]
        static void Main(string[] args)
        {

            Form f = new Program();
            f.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            //f.ShowInTaskbar = false;
            f.StartPosition = FormStartPosition.Manual;
            f.Location = new System.Drawing.Point(-2000, -2000);
            f.Size = new System.Drawing.Size(1, 1);
            Application.Run(f);
            MessageBox.Show("Application Started");


        }
        
        private static string GetDeviceNameNew(DEV_BROADCAST_DEVICEINTERFACE dvi)
        {
            string[] Parts = dvi.dbcc_name.Split('#');
            if (Parts.Length >= 3)
            {
                string DevType = Parts[0].Substring(Parts[0].IndexOf(@"?\") + 2);
                string DeviceInstanceId = Parts[1];
                string DeviceUniqueID = Parts[2];
                string RegPath = @"SYSTEM\CurrentControlSet\Enum\" + DevType + "\\" + DeviceInstanceId + "\\" + DeviceUniqueID;
                RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPath);
                if (key != null)
                {
                    object result = key.GetValue("FriendlyName");
                    if (result != null)
                    {
                        Console.WriteLine("\tNEW Name: {0}", result.ToString());
                        return result.ToString();
                    }
                        
                    result = key.GetValue("DeviceDesc");
                    if (result != null)
                    {
                        Console.WriteLine("\tNEW Desc: {0}", result.ToString());
                        return result.ToString();
                    }
                        
                }
            }
            return String.Empty;
        }

        public Program()
        {
            
            UsbNotification.RegisterUsbDeviceNotification(this.Handle);
        }
        
        public static bool getDetails(String path, ref string pid, ref string vid)
        {
            // PATH=  \\?\USB#VID_062A&PID_4101#<RANDON NUMBERS>
            for(int i = 2; i < path.Length; i++)
            {
                if(path[i-2] == 'P' && path[i-1] == 'I' && path[i] == 'D')
                {
                    i+=2;
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
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            try
            {
                if (m.Msg == WM_DEVICECHANGE)
                {
                    Thread.Sleep(500);
                    DEV_BROADCAST_HDR pHdr = new DEV_BROADCAST_HDR();
                    pHdr = (DEV_BROADCAST_HDR)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_HDR));
                    switch ((int)m.WParam)
                    {

                        case UsbNotification.DbtDeviceremovecomplete:
                                                                                                              
                            Console.WriteLine("Usb_DeviceRemoved");
                            break;


                        case UsbNotification.DbtDevicearrival:
                            Console.WriteLine(" Usb_DeviceAdded"); // this is where you do your magic
                            
                            
                            //Console.WriteLine("Hello: {0}", pHdr.dbch_DeviceType);
                            if (pHdr.dbch_DeviceType == 5)
                            {
                                //Console.WriteLine("Human Interface Device");
                                DEV_BROADCAST_DEVICEINTERFACE pDevInf = (DEV_BROADCAST_DEVICEINTERFACE)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_DEVICEINTERFACE));
                                string name = GetDeviceName(pDevInf);
                                //Console.WriteLine("Name: {0}", name);
                                //GetDeviceNameNew(pDevInf);
                                //Console.WriteLine("Size: {0}\nDevice Type: {1}\nreserved: {2}", pDevInf.dbcc_size, pDevInf.dbcc_devicetype, pDevInf.dbcc_reserved);
                                //Console.WriteLine("Details: {0}", pDevInf.dbcc_name);
                                //Console.WriteLine("Class GUID: {0}", pDevInf.dbcc_classguid);
                                string pid = "";
                                string vid = "";
                                getDetails(pDevInf.dbcc_name.ToString(), ref pid, ref vid);
                                Guid DiskGUID = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");
                                IntPtr h = SetupDiGetClassDevs(ref DiskGUID, new IntPtr(0), IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
                                //Console.WriteLine("Class GUID: {0}", DiskGUID);
                                if (h != (IntPtr)INVALID_HANDLE_VALUE)
                                {

                                    bool Success = true;

                                    uint i = 0;
                                    while (Success)
                                    {
                                        // create a Device Interface Data structure
                                        SP_DEVICE_INTERFACE_DATA dia = new SP_DEVICE_INTERFACE_DATA();
                                        dia.cbSize = Marshal.SizeOf(dia);

                                        // start the enumeration
                                        Success = SetupDiEnumDeviceInterfaces(h, IntPtr.Zero, ref DiskGUID, i, ref dia);

                                        if (Success)
                                        {
                                            // build a DevInfo Data structure
                                            //Console.WriteLine("DiskGUID Agian: " + DiskGUID.ToString());
                                            SP_DEVINFO_DATA da = new SP_DEVINFO_DATA();
                                            da.cbSize = 28; // this becomes 32 for 64 bit systems;

                                            // build a Device Interface Detail Data structure
                                            SP_DEVICE_INTERFACE_DETAIL_DATA didd = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                                            didd.cbSize = 4 + Marshal.SystemDefaultCharSize; // trust me :)

                                            int nRequiredSize = 0;
                                            int nBytes = 256; //Buffer Size = 256
                                            uint n_required_size = (uint)(int)nRequiredSize;
                                            uint nbytes = (uint)(int)nBytes;
                                            if (SetupDiGetDeviceInterfaceDetail(h, ref dia, ref didd, nbytes, out n_required_size, ref da))
                                            {
                                                

                                                // Now we get the InstanceID of the USB level device
                                                IntPtr ptrInstanceBuf = Marshal.AllocHGlobal(nBytes);
                                                CM_Get_Device_ID(da.devInst, ptrInstanceBuf, nBytes, 0);
                                                string InstanceID = Marshal.PtrToStringAuto(ptrInstanceBuf);
                                                //Console.WriteLine("Instance ID: " + da.devInst);

                                                //Console.WriteLine("PID: {0}\nVID: {1}", pid, vid);
                                                string pid_temp = "", vid_temp = "";
                                                int storageFlag = 0;
                                                getDetails(InstanceID.ToString(), ref pid_temp, ref vid_temp);
                                                
                                                if(pid == pid_temp && vid == vid_temp)
                                                {
                                                    additionalFunctions sa = new additionalFunctions();
                                                    tree.node root = new tree.node(da.devInst);
                                                    //sa.buildTree(root);
                                                    //Console.WriteLine("Device Added: {0} \nGetting Details................{1}....{2}....", InstanceID, pid, vid);
                                                    var test = da.devInst;
                                                    Thread th = new Thread(() =>
                                                    {
                                                        
                                                        string serialNo = sd.getSerial(InstanceID);
                                                        //Console.WriteLine("Serial Number: {0}", serialNo);
                                                        sd.search(pid, vid, ref storageFlag,     serialNo, test); //Search details of the Given PID VID From WMI
                                                    });
                                                    th.Start();
                                                    th.Join();
                                                    Marshal.FreeHGlobal(ptrInstanceBuf);
                                                    SetupDiDestroyDeviceInfoList(h);
                                                    break;
                                                    /*
                                                    if (storageFlag == 1)
                                                    {
                                                        Console.WriteLine("TRYING TO EJECT!!-------------------" + da.devInst);
                                                        additionalFunctions.checkEject(da.devInst); //Ejects the USB by calling the CM_Request_Device_Eject function
                                                        Console.WriteLine("------------------------------------");
                                                    }
                                                    */
                                                    /*const int BUFFER_SIZE = 256;
                                                    UInt32 RequiredSize;
                                                    UInt32 RegType = 0;
                                                    byte[] ptrBuffer = new byte[BUFFER_SIZE];
                                                    IntPtr test = new IntPtr(BUFFER_SIZE);
                                                    tree.node root1 = new tree.node(da.devInst);
                                                    //additionalFunctions.buildTree(root1);
                                                   
                                                    if (SetupDiGetDeviceRegistryProperty(h, ref da,(uint) SetupDiGetDeviceRegistryPropertyEnum.SPDRP_DEVICEDESC, out RegType, ptrBuffer, BUFFER_SIZE, out RequiredSize))
                                                    {
                                                        //string ControllerDeviceDesc = Marshal.PtrToStringAuto(ptrBuf);
                                                        //Console.WriteLine("Controller Device Name: {0}", ControllerDeviceDesc);
                                                        SetupDiGetDeviceRegistryProperty(h, ref da, (uint)SetupDiGetDeviceRegistryPropertyEnum.SPDRP_DEVICEDESC, out RegType, ptrBuffer, BUFFER_SIZE, out RequiredSize);
                                                        byte[] data = ptrBuffer;
                                                        IntPtr ptr = Marshal.AllocHGlobal(data.Length);
                                                        try
                                                        {
                                                             
                                                            //Console.WriteLine("Sibling Status: " + CM_Get_Child(out parentDevInt, da.devInst, 0));
                                                            UInt32 current = da.devInst;
                                                            Microsoft.Win32.RegistryValueKind kind;
                                                            uint length = 0;
                                                            

                                                            Program.CM_Get_DevNode_Registry_Property(da.devInst, 0x0000001A, out kind, IntPtr.Zero, ref length, 0);
                                                            IntPtr buffer = Marshal.AllocHGlobal((int)length);
                                                            Program.CM_Get_DevNode_Registry_Property(da.devInst, 0x0000001A, out kind, buffer, ref length, 0);
                                                            byte[] test1 = new byte[BUFFER_SIZE];

                                                            Marshal.Copy(buffer,test1, 0, (int)length);

                                                            //Console.WriteLine("dadas" + test1 + "DASdasd" + buffer);
                                                            //Console.WriteLine("\t\tDevice PATH: " + "\t" + System.Text.Encoding.UTF8.GetString(test1).ToString());

                                                            
                                                            //Console.WriteLine("The New Function------------------\t" + da.devInst);

                                                            //tree.node root = new tree.node(da.devInst);
                                                            //additionalFunctions.buildTree(root);
                                                            
                                                            String ControllerDesc = System.Text.Encoding.UTF8.GetString(ptrBuffer);
                                                            //Console.WriteLine("Details: {0}", ControllerDesc);
                                                            //Console.WriteLine("Sec Details: {0}", ptrBuffer);

                                                            
                                                            

                                                        }
                                                        finally
                                                        {
                                                            Marshal.FreeHGlobal(ptr);
                                                        }

                                                        //Console.WriteLine("Controller: " + RequiredSize);
                                                    }
                                                    else
                                                    {
                                                        if (GetLastError() == 13)
                                                        {
                                                            Console.WriteLine("The Property doesnot exist for the device");
                                                        }
                                                        else
                                                        {
                                                            Console.WriteLine("SetupDiGetDeviceRegistryProperty Error: " + GetLastError().ToString());
                                                        }
                                                    }*/
                                                    
                                                        
                                                    //Console.WriteLine("Pid: {0}\nVid:{1}", pid, vid);
                                                                                                       
                                                }
                                                

                                                Marshal.FreeHGlobal(ptrInstanceBuf);

                                            }

                                        }
                                        i++;

                                    }
                                }
                                SetupDiDestroyDeviceInfoList(h);
                                
                            }

                            break;
                    }
                }
                base.WndProc(ref m);
                
            }
            catch
            {

            }
        }

        //Constants and other declaration        
        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        [DllImport("setupapi.dll")]
        public static extern int CM_Get_Parent(
           out UInt32 pdnDevInst,
           UInt32 dnDevInst,
           int ulFlags
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

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        public static extern int CM_Get_Device_ID(
           UInt32 dnDevInst,
           IntPtr buffer,
           int bufferLen,
           int flags
        );

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(
             IntPtr DeviceInfoSet
        );

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        public static extern bool SetupDiGetHwProfileFriendlyNameEx(
            uint HwProfile,
            ref IntPtr FriendlyName,
            uint FriendlyNameSize,
            out UInt32 RequiredSize,
            IntPtr MachineName,
            UIntPtr Reserved
        );

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SetupDiGetClassDevs(
                                             ref Guid ClassGuid,
                                             IntPtr Enumerator,
                                             IntPtr hwndParent,
                                             uint Flags
                                            );

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr deviceInfoSet,
            ref SP_DEVINFO_DATA deviceInfoData,
            uint property,
            out UInt32 propertyRegDataType,
            byte[]  propertyBuffer, // the difference between this signature and the one above.
            uint propertyBufferSize,
            out UInt32 requiredSize
        );

        enum SetupDiGetDeviceRegistryPropertyEnum : uint
        {
            SPDRP_DEVICEDESC = 0x00000000, // DeviceDesc (R/W)
            SPDRP_HARDWAREID = 0x00000001, // HardwareID (R/W)
            SPDRP_COMPATIBLEIDS = 0x00000002, // CompatibleIDs (R/W)
            SPDRP_UNUSED0 = 0x00000003, // unused
            SPDRP_SERVICE = 0x00000004, // Service (R/W)
            SPDRP_UNUSED1 = 0x00000005, // unused
            SPDRP_UNUSED2 = 0x00000006, // unused
            SPDRP_CLASS = 0x00000007, // Class (R--tied to ClassGUID)
            SPDRP_CLASSGUID = 0x00000008, // ClassGUID (R/W)
            SPDRP_DRIVER = 0x00000009, // Driver (R/W)
            SPDRP_CONFIGFLAGS = 0x0000000A, // ConfigFlags (R/W)
            SPDRP_MFG = 0x0000000B, // Mfg (R/W)
            SPDRP_FRIENDLYNAME = 0x0000000C, // FriendlyName (R/W)
            SPDRP_LOCATION_INFORMATION = 0x0000000D, // LocationInformation (R/W)
            SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0x0000000E, // PhysicalDeviceObjectName (R)
            SPDRP_CAPABILITIES = 0x0000000F, // Capabilities (R)
            SPDRP_UI_NUMBER = 0x00000010, // UiNumber (R)
            SPDRP_UPPERFILTERS = 0x00000011, // UpperFilters (R/W)
            SPDRP_LOWERFILTERS = 0x00000012, // LowerFilters (R/W)
            SPDRP_BUSTYPEGUID = 0x00000013, // BusTypeGUID (R)
            SPDRP_LEGACYBUSTYPE = 0x00000014, // LegacyBusType (R)
            SPDRP_BUSNUMBER = 0x00000015, // BusNumber (R)
            SPDRP_ENUMERATOR_NAME = 0x00000016, // Enumerator Name (R)
            SPDRP_SECURITY = 0x00000017, // Security (R/W, binary form)
            SPDRP_SECURITY_SDS = 0x00000018, // Security (W, SDS form)
            SPDRP_DEVTYPE = 0x00000019, // Device Type (R/W)
            SPDRP_EXCLUSIVE = 0x0000001A, // Device is exclusive-access (R/W)
            SPDRP_CHARACTERISTICS = 0x0000001B, // Device Characteristics (R/W)
            SPDRP_ADDRESS = 0x0000001C, // Device Address (R)
            SPDRP_UI_NUMBER_DESC_FORMAT = 0X0000001D, // UiNumberDescFormat (R/W)
            SPDRP_DEVICE_POWER_DATA = 0x0000001E, // Device Power Data (R)
            SPDRP_REMOVAL_POLICY = 0x0000001F, // Removal Policy (R)
            SPDRP_REMOVAL_POLICY_HW_DEFAULT = 0x00000020, // Hardware Removal Policy (R)
            SPDRP_REMOVAL_POLICY_OVERRIDE = 0x00000021, // Removal Policy Override (RW)
            SPDRP_INSTALL_STATE = 0x00000022, // Device Install State (R)
            SPDRP_LOCATION_PATHS = 0x00000023, // Device Location Paths (R)
            SPDRP_BASE_CONTAINERID = 0x00000024  // Base ContainerID (R)
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public Int32 cbSize;
            public Guid interfaceClassGuid;
            public Int32 flags;
            private UIntPtr reserved;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid classGuid;
            public uint devInst;
            public IntPtr reserved;
        }

        [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean SetupDiGetDeviceInterfaceDetail(
           IntPtr hDevInfo,
           ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
           ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
           UInt32 deviceInterfaceDetailDataSize,
           out UInt32 requiredSize,
           ref SP_DEVINFO_DATA deviceInfoData
        );


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
        public struct NativeDeviceInterfaceDetailData
        {
            public int size;
            public char devicePath;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }

        [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean SetupDiEnumDeviceInterfaces(
           IntPtr hDevInfo,
           IntPtr devInfo,
           ref Guid interfaceClassGuid,
           UInt32 memberIndex,
           ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData
        );

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
        struct DEV_BROADCAST_HDR
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

        private static char DriveMaskToLetter(int mask)
        {
            char letter;
            string drives = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; //1 = A, 2 = B, 3 = C
            int cnt = 0;
            int pom = mask / 2;
            while (pom != 0)    // while there is any bit set in the mask shift it right        
            {
                pom = pom / 2;
                cnt++;
            }
            if (cnt < drives.Length)
                letter = drives[cnt];
            else
                letter = '?';
            Console.WriteLine(letter);

            return letter;
        }



        private static string GetDeviceName(DEV_BROADCAST_DEVICEINTERFACE dvi)
        {
            string[] Parts = dvi.dbcc_name.Split('#');
            if (Parts.Length >= 3)
            {
                string DevType = Parts[0].Substring(Parts[0].IndexOf(@"?\") + 2);
                string DeviceInstanceId = Parts[1];
                string DeviceUniqueID = Parts[2];
                string RegPath = @"SYSTEM\CurrentControlSet\Enum\" + DevType + "\\" + DeviceInstanceId + "\\" + DeviceUniqueID;
                RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPath);
                if (key != null)
                {
                    object result = key.GetValue("FriendlyName");
                    if (result != null)
                    {
                        Console.WriteLine(result);
                        return result.ToString();
                    }
                    result = key.GetValue("DeviceDesc");
                    if (result != null)
                    {
                        Console.WriteLine(result);
                        return result.ToString();
                    }
                }
            }
            return String.Empty;
        }

    }
}
