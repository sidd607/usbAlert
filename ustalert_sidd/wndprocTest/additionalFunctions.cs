using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace wndprocTest
{
    class additionalFunctions
    {

        public string desc = "";

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


        public string deviceDetails()
        {
            return this.desc;
        }

        

        public static bool checkEject(uint devInst)
        {

            int CR_SUCCESS =   0x00000000;
            int DN_REMOVABLE = 0x00004000;
            UInt32 status;
            UInt32 problem;

            if (CR_SUCCESS == CM_Get_DevNode_Status(out status, out problem, devInst, 0) && (DN_REMOVABLE & status) > 0)
            {
                PNP_VETO_TYPE pnp_veto_type;
                System.Text.StringBuilder sb = new System.Text.StringBuilder(255);

                bool success = (CR_SUCCESS == CM_Request_Device_Eject((IntPtr)devInst, out pnp_veto_type, sb, sb.Capacity, 0));
                return success;
                
            }
            else
            {
                return false;
            }

        }

        public int buildTree(tree.node root)
        {
            

            const int BUFFER_SIZE = 256;
            uint length = 0;
            Microsoft.Win32.RegistryValueKind kind;
            byte[] buf = new byte[BUFFER_SIZE];



            UInt32 otherSiblings = 0;
            UInt32 firstChildInst = 0;
            if (Program.CM_Get_Child(out firstChildInst, root.getInst(), 0) == 0)
            {
                root.add(firstChildInst);
                //Program.CM_Get_Device_ID()
                Console.WriteLine("Child Added: ");

                //Get details about first child and then use CM_Get_Sibling to get rest of children
                Program.CM_Get_DevNode_Registry_Property(firstChildInst, 0x00000002, out kind, IntPtr.Zero, ref length, 0);
                IntPtr buffer = Marshal.AllocHGlobal((int)length);

                if (Program.CM_Get_DevNode_Registry_Property(firstChildInst, 0x00000002, out kind, buffer, ref length, 0) == 0)
                {
                    string driver_name = Marshal.PtrToStringAnsi(buffer);
                    Console.WriteLine("\tDRIVER NAME: {0}", driver_name.ToString());
                    desc += " " + driver_name.ToString();

                }
                else
                {
                    Console.WriteLine("\tCM_GET_REGISRTY: Fail");
                }
                buildTree(new tree.node(firstChildInst));



                //Get Details about rest of the Siblings

                while (Program.CM_Get_Sibling(out otherSiblings, firstChildInst, 0) == 0)
                {
                    root.add(otherSiblings);
                    Console.WriteLine("Sibling added: ");

                    Program.CM_Get_DevNode_Registry_Property(firstChildInst, 0x00000002, out kind, IntPtr.Zero, ref length, 0);
                    IntPtr buferTemp = Marshal.AllocHGlobal((int)length);
                    if (Program.CM_Get_DevNode_Registry_Property(firstChildInst, 0x00000002, out kind, buferTemp, ref length, 0) == 0)
                    {
                        string driver_name = Marshal.PtrToStringAnsi(buferTemp);
                        Console.WriteLine("\tDRIVER NAME: {0}", driver_name.ToString());
                        desc += " " + driver_name.ToString();

                    }
                    else
                    {
                        Console.WriteLine("\tCM_GET_REGISRTY: Fail");
                        Console.WriteLine("Error: " + Program.CM_Get_Sibling(out otherSiblings, firstChildInst, 0));
                    }

                    buildTree(new tree.node(otherSiblings));

                    firstChildInst = otherSiblings;
                    otherSiblings = 0;

                }

                return 0;
            }
            else
            {
                Console.WriteLine("No Child ! !");
                return 10;
            }

        }

    }
}
