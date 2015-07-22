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

        void LoadLicFile(out string p_lictypedesc)
        {
            p_lictypedesc = "3";
            string versiontypestr = "";
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load("C:\\src\\Inteli-Phone-Book\\InteliPhoneBook\\InteliPhoneBookService\\bin\\Debug\\lic.xml");

            string endpoint = xdoc.DocumentElement["endpoint"].InnerText;
            AddKey = DecryptDES_ProjectInsideKey(endpoint, "35405717");
            /*xdoc.DocumentElement["type"].InnerText;
            xdoc.DocumentElement["createtime"].InnerText;
            xdoc.DocumentElement["endtime"].InnerText;
            xdoc.DocumentElement["guid"].InnerText;*/
            XmlElement elem = (XmlElement)xdoc.DocumentElement["features"].FirstChild;
            int nFeatures = xdoc.DocumentElement["features"].GetElementsByTagName("feature").Count;
            for (int i = 0; i < nFeatures; i++)
            {
                if (elem.Attributes["name"].Value == "versiontype")
                    versiontypestr = DecryptDES(elem.Attributes["value"].Value, "35405717");
                elem = (XmlElement)elem.NextSibling;
            }
            //xdoc.DocumentElement["signature"].InnerText;

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
                log.Info("evaluation lic mode." + versiontypestr);
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
            string lictypedesc;
            LicProcessor licProcessor = (LicProcessor)stateInfo;
            LicProcessor.LicProcessorObj = licProcessor;
            
            while (true)
            {
                if (InteliPhoneBookService.ServiceIsTerminating == 1)
                { Interlocked.Increment(ref InteliPhoneBookService.LicThreadTerminated); break; }
                LicProcessorObj.LoadLicFile(out lictypedesc);
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
