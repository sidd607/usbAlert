using System;
using System.IO;
using System.Management;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wndprocTest
{
    class searchData
    {
        public bool eject = false;
        public static bool get(string path, ref string pid, ref string vid)
        {

            pid = "";
            vid = "";
            if (path.Contains("VID_") && path.Contains("PID_"))
            {
                //Console.WriteLine(path);
                path = path.Split('\\')[2];
                //Console.WriteLine(path);
                string tempVid = path.Split('_')[1];
                string tempPid = path.Split('_')[2];
                tempVid = tempVid.Split('&')[0];
                tempPid = tempPid.Split('&')[0];
                pid = tempPid;
                vid = tempVid;
                // Console.WriteLine("PID: {0},\tVID:{1}", pid, vid);
                return true;
            }
            else
            {
                //Console.WriteLine("SORRY!!");
                return false;
            }
        }

        public static void getId(ref string devid)
        {
            devid = devid.Split('=')[1];
            // Console.WriteLine("devID {0}", devid);
            devid = devid.Split('"')[1];
            //  Console.WriteLine("DEvID {0}", devid);
        }

        public static void getPVID(string devid, ref string pid, ref string vid)
        {
            for (int i = 2; i < devid.Length; i++)
            {
                if ((devid[i] == 'P' || devid[i] == 'p') && (devid[i + 1] == 'I' || devid[i + 1] == 'i') && (devid[i + 2] == 'D' && devid[i + 2] == 'd'))
                {
                    for (int j = 4; j < 8; j++)
                    {
                        pid += devid[i + j];
                    }
                }
                else if ((devid[i] == 'V' || devid[i] == 'v') && (devid[i + 1] == 'I' || devid[i + 1] == 'i') && (devid[i + 2] == 'D' && devid[i + 2] == 'd'))
                {
                    for (int j = 4; j < 8; j++)
                    {
                        vid += devid[i + j];
                    }
                }
            }
            //Console.WriteLine("PID: {0}\tVID: {1}", pid, vid);
        }

        public string getSerial(string instanceID)
        {
            try
            {

                if (instanceID.Split('\\')[2].Contains('&'))
                    return "";
                else
                    return instanceID.Split('\\')[2];
            }
            catch
            {
                return "";
            }
        }

        private static int checkType(string name)
        {
            name = name.ToLower();
            if (name == "usb mass storage device")
                return 1;
            else if (name == "usb composite device")
                return 2;
            else if (name.Contains("network") || name.Contains("wireless") || name.Contains("wireless adapter") || name.Contains("802.11"))
                return 3;
            else
                return 0;
        }

        public  void search(string productID, string vendorID, ref int StorageFlag, string serialNo, UInt32 test)
        {

            Console.WriteLine("Getting Details for " + productID + " & " + vendorID);
            int flag = 0;
            string name = "", finalDisp = "", tempName = "";
            int type = 0;
            int tempFlag = 0;
            int compositeFlag = 0;
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_USBControllerDevice");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    //Console.WriteLine("-----------------------------------");
                    //Console.WriteLine("Win32_USBControllerDevice instance");
                    //Console.WriteLine("-----------------------------------");
                    //Console.WriteLine("Dependent: {0}", queryObj["Dependent"]);
                    string devid = (string)queryObj["Dependent"];
                    getId(ref devid);

                    try
                    {
                        ManagementObjectSearcher searcher2 = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE  '%" + devid + "%' ");
                        foreach (ManagementObject q in searcher2.Get())
                        {

                            string pid = "1", vid = "2";
                            //Console.WriteLine("-------------- FOUND SOMETHING -----------");
                            get(devid, ref pid, ref vid);
                            if (pid == productID && vid == vendorID && flag == 0)
                            {

                                //Console.WriteLine("-------------- FOUND SOMETHING -----------");
                                // Console.WriteLine("PnPEntity");
                                //Console.WriteLine("DeviceID: {0}", q["DeviceID"]);
                                // Console.WriteLine("Class GUID: {0}", q["ClassGuid"]);


                                //Console.WriteLine("Name: {0}", q["Name"]);
                                name = q["Name"].ToString();
                                type = checkType(name);
                                //Console.WriteLine("TYPE: " + type);
                                finalDisp += pid + ", " + vid + ", " + serialNo + ", ";
                                
                                // Console.WriteLine("PID: {0}\tVID: {1}", pid, vid);
                                flag = 1;
                                //Console.WriteLine("-------------- FOUND SOMETHING -----------");

                            }

                            if (flag == 1)
                            {
                                switch (type)
                                {
                                    case 1:                 //This is for pendrives and mass storage devices
                                                            //string temp = q["Name"].ToString();

                                        if (tempFlag == 1)
                                        {
                                            DateTime now = DateTime.Now;
                                            finalDisp += q["Name"].ToString() + ", " + "REMOVABLE MEDIA" + ", " + now;
                                            StorageFlag = 1;
                                        }
                                        tempFlag++;
                                        break;

                                    case 2:                //This is for composite devices

                                        if (tempFlag == 1)
                                        {

                                            if (q["Name"].ToString() == "USB Input Device")
                                            {
                                                /// Check for Input devices
                                                compositeFlag = 1;

                                            }
                                            else
                                            {
                                                // check for smart phones
                                                compositeFlag = 2;

                                            }
                                        }

                                        if (tempFlag > 1 && compositeFlag == 2) // get Details for Smart phones
                                        {
                                            if (tempFlag == 3)
                                            {
                                                DateTime now = DateTime.Now;
                                                finalDisp += q["Name"].ToString() + ", " + "SmartPhone" + ", " + now;
                                                StorageFlag = 1;
                                            }
                                        }

                                        if (tempFlag > 1 && compositeFlag == 1)
                                        {
                                            if (pid == productID && vid == vendorID)
                                            {
                                                tempName += q["Name"].ToString() + " ";
                                            }
                                            else
                                            {
                                                tempName = tempName.ToLower();
                                                if (tempName.Contains("mouse"))
                                                {
                                                    DateTime now = DateTime.Now;
                                                    finalDisp += "USB Input Device " + q["Manufacturer"] + ", " + "USB MOUSE" + ", " + now;
                                                    StorageFlag = 0;
                                                    compositeFlag = 4;
                                                }
                                                else if (tempName.Contains("keyboard"))
                                                {
                                                    DateTime now = DateTime.Now;
                                                    finalDisp += "USB Input Device " + q["Manufacturer"] + ", " + "USB KEYBOARD" + ", " + now;
                                                    StorageFlag = 0;
                                                    compositeFlag = 4;
                                                }
                                                else
                                                {
                                                    DateTime now = DateTime.Now;
                                                    finalDisp += "USB Input Device " + q["Manufacturer"] + ", " + "Input Device" + ", " + now;
                                                    StorageFlag = 0;
                                                    compositeFlag = 4;
                                                }
                                            }
                                        }
                                        tempFlag++;
                                        break;
                                    case 3:
                                        if (tempFlag == 0)
                                        {
                                            DateTime now = DateTime.Now;
                                            finalDisp += name + ", " + "Network Interface Card" + ", " + now;
                                            StorageFlag = 0;
                                        }
                                        tempFlag++;
                                        break;
                                    default:
                                        finalDisp += name + ", " + "SmartPhone" + ", " + DateTime.Now;
                                        StorageFlag = 1;

                                        break;
                                }
                            }

                        }
                        if(StorageFlag == 1)
                        {
                            //Console.WriteLine("TRYING TO EJECT!!-------------------" + test);
                            eject=additionalFunctions.checkEject(test); //Ejects the USB by calling the CM_Request_Device_Eject function
                            //Console.WriteLine("------------------------------------");
                        }
                    }
                    catch (ManagementException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            catch (ManagementException e)
            {
                Console.WriteLine("An error occurred while querying for WMI data: " + e.Message);
            }
            if (flag == 0)
            {
                Console.WriteLine("Couldnot find the required device");
            }
            finalDisp += System.Environment.NewLine.ToString();
            Console.WriteLine("LOG: " + finalDisp);
            
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "Logs.txt", finalDisp);
            try
            {
                Console.WriteLine("\n\nDevice Details ------------------------------------");
                Console.WriteLine("PID: " + finalDisp.Split(',')[0]);
                Console.WriteLine("VID: " + finalDisp.Split(',')[1]);
                if(serialNo != "")
                    Console.WriteLine("Serial No: " + finalDisp.Split(',')[2]);
                try
                {
                    Console.WriteLine("Type: " + finalDisp.Split(',')[4]);
                }
                catch
                {
                    Console.WriteLine("TYPE ERROR");
                }

                Console.Write("Mass Storage: ");
                if (StorageFlag == 1)
                    Console.Write("True\n");
                else
                    Console.Write("False\n");
                Console.WriteLine("Logged: " + DateTime.Now);
                Console.WriteLine("---------------------------------------------------\n");
                if(eject == true && StorageFlag == 1)
                {
                    Console.WriteLine("Eject-> True\n");
                }
                else if(StorageFlag == 1 && eject == false)
                    Console.WriteLine("Eject-> Fail\n");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

        }

        public static void myMain(string[] args)
        {

            Console.WriteLine("________________________________");
            
            while (true) ;
        }
    }
}
