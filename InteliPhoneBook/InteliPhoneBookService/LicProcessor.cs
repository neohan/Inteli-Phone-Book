using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data;
using System.Data.SqlClient;

namespace InteliPhoneBookService
{
    class LicProcessor
    {
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

        static public void DoWork(Object stateInfo)
        {
            LicProcessor licProcessor = (LicProcessor)stateInfo;
            LicProcessor.LicProcessorObj = licProcessor;
            
            while (true)
            {
                if (InteliPhoneBookService.ServiceIsTerminating == 1)
                { Interlocked.Increment(ref InteliPhoneBookService.LicThreadTerminated); break; }
                LicProcessorObj.UpdateLicInfo("4", "5");
                Thread.Sleep(1000);
            }
            log.Info("exited\r\n");
        }
    }
}
