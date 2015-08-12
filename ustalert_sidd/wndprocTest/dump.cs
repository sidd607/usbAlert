using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wndprocTest
{
    class dump
    {
    }
}





//Console.WriteLine("Hello: {0}",pHdr.dbch_DeviceType);
/*
if(pHdr.dbch_DeviceType == 5)
{
    //Console.WriteLine("Human Interface Device");
    DEV_BROADCAST_DEVICEINTERFACE pDevInf = (DEV_BROADCAST_DEVICEINTERFACE)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_DEVICEINTERFACE));
    //Console.WriteLine("Size: {0}\nDevice Type: {1}\nreserved: {2}", pDevInf.dbcc_size, pDevInf.dbcc_devicetype, pDevInf.dbcc_reserved);
    Console.WriteLine("Details: {0}", pDevInf.dbcc_name);
    string pid = "";
    string vid = "";
    getDetails(pDevInf.dbcc_name.ToString(), ref pid, ref vid);
    Console.WriteLine("PID: {0}\nVID: {1}", pid, vid);
   // Console.WriteLine("Class GUID: {0}", pDevInf.dbcc_classguid);
    Guid DiskGUID = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED" );
    IntPtr h = SetupDiGetClassDevs(ref DiskGUID, new IntPtr(0), IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
    if (h != (IntPtr)INVALID_HANDLE_VALUE)
    {
        //Console.WriteLine("DISK GUID" + DiskGUID.ToString());
        bool Success = true;
        uint i = 0;
        while (Success)
        {
            // create a Device Interface Data structure
            SP_DEVICE_INTERFACE_DATA dia = new SP_DEVICE_INTERFACE_DATA();
            dia.cbSize = Marshal.SizeOf(dia);

            // start the enumeration
            Success = SetupDiEnumDeviceInterfaces(h, IntPtr.Zero, ref DiskGUID, i, ref dia);
            //Console.WriteLine("\nDiskGUID: {0}\ndia: {1}\nh: {2}\n", DiskGUID.ToString(), dia.interfaceClassGuid, h.ToString());

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
                    // current InstanceID is at the "USBSTOR" level, so we
                    // need up "move up" one level to get to the "USB" level
                    uint ptrPrevious; // Should have been IntPtr

                    CM_Get_Parent(out ptrPrevious, da.devInst, 0);

                    // Now we get the InstanceID of the USB level device   
                    IntPtr ptrInstanceBuf = Marshal.AllocHGlobal(nBytes);
                    CM_Get_Device_ID(da.devInst, ptrInstanceBuf, nBytes, 0);
                    string InstanceID = Marshal.PtrToStringAuto(ptrInstanceBuf);

                    string pid_temp = "", vid_temp = "";

                    getDetails(InstanceID.ToString(), ref pid_temp, ref vid_temp);
                    if(pid == pid_temp && vid == vid_temp)
                    {
                     //   Console.WriteLine("This is it: {0}", InstanceID);
                    }

                    //Console.WriteLine("PID: {0}\nVID: {1}", pid, vid);

                    Marshal.FreeHGlobal(ptrInstanceBuf);
                }

            }
            i++;

        }
    }
    SetupDiDestroyDeviceInfoList(h);


}
else if(pHdr.dbch_DeviceType == 0)
{
    Console.WriteLine("adjasjflas");
}
*/
