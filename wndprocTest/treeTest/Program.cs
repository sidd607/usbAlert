using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace treeTest
{
    class tree
    {

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

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        static extern int CM_Request_Device_Eject(IntPtr devinst, out PNP_VETO_TYPE pVetoType, System.Text.StringBuilder pszVetoName, int ulNameLength, int ulFlags);


        [DllImport("setupapi.dll", SetLastError = true)]
        static extern int CM_Get_DevNode_Status(out UInt32 status, out UInt32 probNum, UInt32 devInst, int flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern int CM_Get_DevNode_Registry_Property(
            uint deviceInstance,
            uint property,
            out Microsoft.Win32.RegistryValueKind pulRegDataType,
            IntPtr buffer,
            ref uint length,
            uint flags);

        public class node
        {
            public UInt32 instID;
            List<node> children = new List<node>();
            public int size;
            public node parent;

            public node(UInt32 instID)
            {
                this.instID = instID;
                size = 0;
                parent = null;
            }

            public UInt32 getInst()
            {
                return this.instID;
            }

            public int setParent(node parent)
            {
                try
                {
                    this.parent = parent;
                    return 0;
                }
                catch
                {
                    return 1;
                }
            }

            public int add(UInt32 childIst)
            {
                node child = new node(childIst);
               
                try
                {
                    this.children.Add(child);
                    size++;
                    return 0;
                }
                catch
                {
                    return 1;
                }
            }

            public List<node> getAll(ref int si)
            {
                si = this.size;
                return children;
            }

        }

        public static void disp(node root)
        {
            Console.WriteLine(root.getInst());
            int size = 0;
            List<node> children = new List<node>();
            children = root.getAll(ref size);
            
            for(int i = 0; i < size; i++)
            {
                disp(children[i]);
            }
        }

        public static void Main(String[] args)
        {
            //Ejectting a device
            IntPtr devinst = new IntPtr(0);
            devinst = (IntPtr)4;
            int CR_SUCCESS = 0x00000000;
            int DN_REMOVABLE = 0x00004000;
            UInt32 status = 0;
            UInt32 problem = 0;
            UInt32 den = 4;
            Microsoft.Win32.RegistryValueKind kind;
            UInt32 length = 0;

            CM_Get_DevNode_Registry_Property(den, 0x00000002, out kind, IntPtr.Zero, ref length, 0);
            IntPtr buffer = Marshal.AllocHGlobal((int)length);
            CM_Get_DevNode_Registry_Property(den, 0x00000002, out kind, buffer, ref length, 0);
            Console.WriteLine("Registy Prperty: " + Marshal.PtrToStringAnsi(buffer));

            if (CR_SUCCESS == CM_Get_DevNode_Status(out status, out problem, den, 0) && (DN_REMOVABLE & status) > 0)
            {
                PNP_VETO_TYPE pnp_veto_type;
                System.Text.StringBuilder sb = new System.Text.StringBuilder(255);

                bool success = (CR_SUCCESS == CM_Request_Device_Eject(devinst, out pnp_veto_type, sb, sb.Capacity, 0));

                Console.WriteLine("EJECT: " + success.ToString());
            }
            Console.WriteLine("Status False");
            Console.Read();
        }
    }
}