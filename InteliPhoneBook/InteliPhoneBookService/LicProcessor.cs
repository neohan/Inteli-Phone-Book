using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Management;
using System.Net.NetworkInformation;

namespace InteliPhoneBookService
{
    class LicProcessor
    {
        private static byte[] ProjectInsideKeys = { 0xAA, 0xDF, 0x29, 0x5C, 0x36, 0x90, 0x17, 0x68 };
        private static string ProjectInsideAddKey = "projectINSIDEkey";
        private static byte[] Keys = { 0xA5, 0x95, 0x6E, 0x44, 0x80, 0xCB, 0x8F, 0x77 };
        private static string AddKey = "";

        private const string INSERT_TABLE = " SystemConfig ";
        private const string INSERT_PARAMS = " (CfgKey, CfgValue, DisplayName) VALUES(@cfgkey,@cfgvalue,@displayname) ";
        private const string UPDATE_WHERES = " WHERE CfgKey = @keyname ";
        static public log4net.ILog log = log4net.LogManager.GetLogger("lic");
        static public LicProcessor LicProcessorObj = null;

        private static string GetCpuId()
        {
            string strCpuid = "";
            try
            {
                ManagementClass mcCpu = new ManagementClass("win32_Processor");
                ManagementObjectCollection mocCpu = mcCpu.GetInstances();

                foreach (ManagementObject m in mocCpu)
                {
                    strCpuid = m["ProcessorId"].ToString();
                    if (strCpuid != null)
                    {
                        break;
                    }
                }

                return strCpuid;
            }
            catch
            {
                return strCpuid;
            }
        }

        private static string GetDiskId()
        {
            string diskId = "";

            try
            {
                ManagementObjectSearcher wmiSearcher = new ManagementObjectSearcher();

                wmiSearcher.Query = new SelectQuery("Win32_DiskDrive",
                                                    "",
                                                    new string[] { "PNPDeviceID" });
                ManagementObjectCollection myCollection = wmiSearcher.Get();
                ManagementObjectCollection.ManagementObjectEnumerator em =
                myCollection.GetEnumerator();
                em.MoveNext();
                ManagementBaseObject mo = em.Current;
                diskId = mo.Properties["PNPDeviceID"].Value.ToString().Trim();
                return diskId;
            }
            catch
            {
                return diskId;
            }
        }

        private static string GetMacId()
        {
            string macId = "";

            try
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    NetworkInterface[] ifaces = NetworkInterface.GetAllNetworkInterfaces();
                    PhysicalAddress address = ifaces[0].GetPhysicalAddress();
                    byte[] byteAddr = address.GetAddressBytes();
                    for (int i = 0; i < byteAddr.Length; i++)
                    {
                        macId += byteAddr[i].ToString("X2");
                        if (i != byteAddr.Length - 1)
                        {
                            macId += "-";
                        }
                    }
                }
                return macId;
            }
            catch
            {
                return macId;
            }
        }


        public bool UpdateLicInfo(string p_featuretype, string p_lictype)
        {
            int result;
            bool bFound = false;

            StringBuilder strSQL = new StringBuilder();
            strSQL.Append("SELECT CfgValue FROM SystemConfig WHERE CfgKey = \'FS_ESL_NBMODE_NETTIMES\' ");
            try
            {
                using (SqlDataReader rdr = SqlHelper.ExecuteReader(SqlHelper.SqlconnString, CommandType.Text, strSQL.ToString(), null))
                {while (rdr.Read()) { bFound = true; }}
            }
            catch (Exception e){log.Info(String.Format("Error occurs during execute sql:{0}.\r\n{1}\r\n", strSQL.ToString(), e.Message));}

            if (bFound == false)
            {
                try
                {
                    strSQL.Clear();
                    strSQL.Append("INSERT INTO ").Append(INSERT_TABLE).Append(INSERT_PARAMS);

                    SqlParameter[] parms = new SqlParameter[] {
                        new SqlParameter("@cfgkey", "FS_ESL_NBMODE_NETTIMES"),
                        new SqlParameter("@cfgvalue", "3"),
                        new SqlParameter("@displayname", "FS_ESL_NBMODE_NETTIMES")};
                    SqlHelper.ExecuteNonQuery(strSQL.ToString(), out result, parms);
                }
                catch (Exception e) { log.Info(String.Format("Error occurs during execute sql:{0}.\r\n{1}\r\n", strSQL.ToString(), e.Message)); }
            }

            bFound = false; strSQL.Clear();
            strSQL.Append("SELECT CfgValue FROM SystemConfig WHERE CfgKey = \'FS_ESL_NBMODE_CONSECONDS\' ");
            try
            {
                using (SqlDataReader rdr = SqlHelper.ExecuteReader(SqlHelper.SqlconnString, CommandType.Text, strSQL.ToString(), null))
                { while (rdr.Read()) { bFound = true; } }
            }
            catch (Exception e) { log.Info(String.Format("Error occurs during execute sql:{0}.\r\n{1}\r\n", strSQL.ToString(), e.Message)); }

            if (bFound == false)
            {
                try
                {
                    strSQL.Clear();
                    strSQL.Append("INSERT INTO ").Append(INSERT_TABLE).Append(INSERT_PARAMS);

                    SqlParameter[] parms = new SqlParameter[] {
                        new SqlParameter("@cfgkey", "FS_ESL_NBMODE_CONSECONDS"),
                        new SqlParameter("@cfgvalue", "3"),
                        new SqlParameter("@displayname", "FS_ESL_NBMODE_CONSECONDS")};
                    SqlHelper.ExecuteNonQuery(strSQL.ToString(), out result, parms);
                }
                catch (Exception e) { log.Info(String.Format("Error occurs during execute sql:{0}.\r\n{1}\r\n", strSQL.ToString(), e.Message)); }
            }

            try
            {
                strSQL.Clear();
                strSQL.Append("UPDATE SystemConfig SET CfgValue = @cfgvalue ").Append(UPDATE_WHERES);

                SqlParameter[] parms = new SqlParameter[] {
                    new SqlParameter("@cfgvalue", p_featuretype),
                    new SqlParameter("@keyname", "FS_ESL_NBMODE_NETTIMES") };

                SqlHelper.ExecuteNonQuery(strSQL.ToString(), out result, parms);
            }
            catch (Exception e)
            {
                log.Info(String.Format("Error occurs during execute sql:{0}.\r\n{1}\r\n", strSQL.ToString(), e.Message));
            }
            try
            {
                strSQL.Clear();
                strSQL.Append("UPDATE SystemConfig SET CfgValue = @cfgvalue ").Append(UPDATE_WHERES);

                SqlParameter[] parms = new SqlParameter[] {
                    new SqlParameter("@cfgvalue", p_lictype),
                    new SqlParameter("@keyname", "FS_ESL_NBMODE_CONSECONDS") };

                SqlHelper.ExecuteNonQuery(strSQL.ToString(), out result, parms);
            }
            catch (Exception e)
            {
                log.Info(String.Format("Error occurs during execute sql:{0}.\r\n{1}\r\n", strSQL.ToString(), e.Message));
            }
            return bFound;
        }

        void LoadLicFile(out string p_lictypedesc, out string p_siptrunk)
        {
            string thisEndpointKey = EncryptDES_ProjectInsideKey(String.Format("{0}-{1}-{2}", GetCpuId(), GetDiskId(), GetMacId()), "35405717");
            p_lictypedesc = "0"; p_siptrunk = "1";
            string versiontypestr = "";
            XmlDocument xdoc = new XmlDocument();
            try
            {
                int pos = Application.ExecutablePath.LastIndexOf("\\");
                string path = Application.ExecutablePath.Substring(0, pos);
                xdoc.Load(path + "\\lic.xml");
            }
            catch (Exception ex)
            {
                p_lictypedesc = "3";
                log.Info(String.Format("invalid lic.\r\n{0}", ex.Message));
                return;
            }

            //verification
            try
            {
                string endpoint = xdoc.DocumentElement["endpoint"].InnerText;
                if (endpoint != thisEndpointKey)
                {
                    p_lictypedesc = "0";
                    log.Info(String.Format("invalid lic.machine info is wrong.\r\nresult   :{0}\r\nsignature:{1}", thisEndpointKey, endpoint));
                    return;
                }
                AddKey = DecryptDES_ProjectInsideKey(endpoint, "35405717");

                SHA384Managed shaM = new SHA384Managed();
                byte[] data;

                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(12);
                bw.Write(xdoc.DocumentElement["level"].InnerText);
                bw.Write(xdoc.DocumentElement["type"].InnerText);
                bw.Write(xdoc.DocumentElement["endpoint"].InnerText);
                bw.Write(xdoc.DocumentElement["createtime"].InnerText);
                bw.Write(xdoc.DocumentElement["endtime"].InnerText);
                bw.Write(xdoc.DocumentElement["guid"].InnerText);
                XmlElement elem = (XmlElement)xdoc.DocumentElement["features"].FirstChild;
                int nFeatures = xdoc.DocumentElement["features"].GetElementsByTagName("feature").Count;
                for (int i = 0; i < nFeatures; i++)
                {
                    bw.Write(elem.Attributes["name"].Value); bw.Write(elem.Attributes["value"].Value);
                    if (elem.Attributes["name"].Value == "versiontype")
                        versiontypestr = DecryptDES(elem.Attributes["value"].Value, "35405717");
                    if (elem.Attributes["name"].Value == "siptrunk")
                        p_siptrunk = DecryptDES(elem.Attributes["value"].Value, "35405717");
                    elem = (XmlElement)elem.NextSibling;
                }
                int nLen = (int)ms.Position + 1;
                bw.Close();
                ms.Close();
                data = ms.GetBuffer();

                data = shaM.ComputeHash(data, 0, nLen);

                string result = "";
                foreach (byte dbyte in data)
                {
                    result += dbyte.ToString("X2");
                }
                string signature = xdoc.DocumentElement["signature"].InnerText;
                if (signature != result)
                {
                    p_lictypedesc = "0";
                    log.Info(String.Format("invalid lic.signature is wrong.\r\nresult   :{0}\r\nsignature:{1}", result, signature));
                    return;
                }
            }
            catch (Exception ex)
            {
                p_lictypedesc = "0";
                log.Info(String.Format("invalid lic info.\r\n{0}", ex.Message));
                return;
            }


            if (versiontypestr == "basic")//SJ966WcqFE8=
            {
                p_lictypedesc = "4";
                log.Info("basic lic mode");
            }
            else if (versiontypestr == "enforce")//RH2ah1pD+oo=
            {
                p_lictypedesc = "5";
                log.Info("enforce lic mode");
            }
            else if (versiontypestr == "custom")//EpeEkWdlzb4=
            {
                p_lictypedesc = "6";
                log.Info("custom lic mode");
            }
            else
            {
                p_lictypedesc = "0";
                log.Info("invalid lic." + versiontypestr);
            }
        }

        string DecryptDES_ProjectInsideKey(string decryptString, string decryptKey)
        {

            try
            {
                if (decryptKey.Length < 8)
                {
                    decryptKey += ProjectInsideAddKey;
                }
                byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey.Substring(0, 8));

                byte[] rgbIV = ProjectInsideKeys;

                byte[] inputByteArray = Convert.FromBase64String(decryptString);

                DESCryptoServiceProvider DCSP = new DESCryptoServiceProvider();

                MemoryStream mStream = new MemoryStream();

                CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, rgbIV), CryptoStreamMode.Write);

                cStream.Write(inputByteArray, 0, inputByteArray.Length);

                cStream.FlushFinalBlock();

                return Encoding.UTF8.GetString(mStream.ToArray());

            }
            catch
            {
                return decryptString;
            }
        }

        private static string EncryptDES_ProjectInsideKey(string encryptString, string encryptKey)
        {
            try
            {
                if (encryptKey.Length < 8)
                {
                    encryptKey += ProjectInsideAddKey;
                }

                byte[] rgbKey = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));

                byte[] rgbIV = ProjectInsideKeys;

                byte[] inputByteArray = Encoding.UTF8.GetBytes(encryptString);

                DESCryptoServiceProvider dCSP = new DESCryptoServiceProvider();

                MemoryStream mStream = new MemoryStream();

                CryptoStream cStream = new CryptoStream(mStream, dCSP.CreateEncryptor(rgbKey, rgbIV), CryptoStreamMode.Write);

                cStream.Write(inputByteArray, 0, inputByteArray.Length);

                cStream.FlushFinalBlock();

                return Convert.ToBase64String(mStream.ToArray());

            }
            catch
            {
                return encryptString;
            }
        }

        string DecryptDES(string decryptString, string decryptKey)
        {

            try
            {
                if (decryptKey.Length < 8)
                {
                    decryptKey += AddKey;
                }
                byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey.Substring(0, 8));

                byte[] rgbIV = Keys;

                byte[] inputByteArray = Convert.FromBase64String(decryptString);

                DESCryptoServiceProvider DCSP = new DESCryptoServiceProvider();

                MemoryStream mStream = new MemoryStream();

                CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, rgbIV), CryptoStreamMode.Write);

                cStream.Write(inputByteArray, 0, inputByteArray.Length);

                cStream.FlushFinalBlock();

                return Encoding.UTF8.GetString(mStream.ToArray());

            }
            catch
            {
                return decryptString;
            }
        }

        static public void DoWork(Object stateInfo)
        {
            string lictypedesc, siptrunks;
            LicProcessor licProcessor = (LicProcessor)stateInfo;
            LicProcessor.LicProcessorObj = licProcessor;
            FileInfo fi = new FileInfo(Application.ExecutablePath);
            log.Info("date:" + fi.CreationTime.ToString());
            DateTime now;
            
            while (true)
            {
                if (InteliPhoneBookService.ServiceIsTerminating == 1)
                { Interlocked.Increment(ref InteliPhoneBookService.LicThreadTerminated); break; }
                LicProcessorObj.LoadLicFile(out lictypedesc, out siptrunks);
                
                now = DateTime.Now;
                TimeSpan span = now - fi.CreationTime;
                if (span.Days > 30)
                {
                    log.Info("evaluation lic excced limit:" + span.Days);
                    if (lictypedesc == "3")
                    {
                        LicProcessorObj.UpdateLicInfo("3", "3");
                    }
                }

                if ( lictypedesc == "0" )
                    LicProcessorObj.UpdateLicInfo("3", lictypedesc);
                else
                    LicProcessorObj.UpdateLicInfo("4", lictypedesc);
                Thread.Sleep(60000);
            }
            log.Info("exited\r\n");
        }
    }
}
