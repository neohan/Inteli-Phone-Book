using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using System.IO;
using System.Threading;

namespace IPBDialWSTortureTest
{
    public class ClickToDial
    {
        public string TaskID;
        public string WSIP;
        public string SIPGatewayIP;
        public string SIPGatewayPort;
        public string SIPServerIP;
        public string SIPServerPort;
        public string SIPServerAddress;
        public string SIPServerIPBackup;
        public string SIPServerPortBackup;
        public string SIPServerAddressBackup;
        public string Ani;
        public string Dnis;
        public string Uuid;
        public string UserID;

        public DateTime CreateTime;                 //记录创建时间，是为了在超时时长后丢弃此实例。
        public string CurrentStatus;
        public bool bFinished;
    }

    class DialWSTortureTest
    {
        public static log4net.ILog log = log4net.LogManager.GetLogger("tor");

        public bool bFSUp;
        public int TotalSend = 0;
        public int TotalConnectWSServer = 0;
        public int TotalCannotConnectWSServer = 0;
        public int TotalConnectWSServerPredict = 0;
        public int TotalCannotConnectWSServerPredict = 0;
        public string[] ANI = { "4001", "4002", "4003", "4003", "4004", "4005", "4006", "4007", "4008", "4009", "4010", "4011", "4012" };
        public string[] USERID = { "xlksd-4001-8792993-klsdjfl", "iepoolc-4002-8909923", "xmnvck-4003-545290i", "aksdlc-4003-87993", "zmkcoie-4004-873,md", "mlkcje-4005-8iiemc8", "akkm-4006-87793", "mkueyt-4007-9773uue", "nxcjjsu-4008-7iuekmd", "cmjcie-4009-8i3ijmnd", "mcjed-4010-883idm", "zmckjd-4011-83kedmmd", "qaewre-4012-1qdd" };
        public string[] DNIS = { "4002", "4003", "4003", "4004", "4005", "4006", "4007", "4008", "4009", "4010", "4011", "4012", "4013", "4014" };
        public string[] WSIP = { "192.168.77.168", "192.168.77.169" };
        public string[] SipGWIP = { "192.168.77.167", "192.168.77.168", "192.168.77.169" };
        public string[] SipSVRIP = { "192.168.77.250", "192.168.77.251", "192.168.77.252", "192.168.77.253" };
        public string[] SipSVRPort = { "5058", "5059", "5060", "5061", "5062" };
        public string[] SipSVRIPBackup = { "192.168.77.248", "192.168.77.249", "192.168.77.250", "192.168.77.251", "192.168.77.252", "192.168.77.253" };
        public string[] SipSVRPortBackup = { "5050", "5051", "5052", "5060", "5063", "5064", "5065" };
        public int ANIIndex = 0;
        public int DNISIndex = 0;
        public int USERIDIndex = 0;
        public int WSIPIndex = 0;
        public int SipGWIPIndex = 0;
        public int SipSVRIPIndex = 0;
        public int SipSVRPortIndex = 0;
        public int SipSVRIPBackupIndex = 0;
        public int SipSVRPortBackupIndex = 0;
        public string ID;

        public Dictionary<string, ClickToDial> taskList = new Dictionary<string, ClickToDial>();
        public List<string> taskIdList = new List<string>();

        #region
        string GetANI()
        {
            if (ANIIndex >= ANI.Count())
                ANIIndex = 0;
            int index = ANIIndex;
            ++ANIIndex;
            return ANI[index];
        }

        string GetDNIS()
        {
            if (DNISIndex >= DNIS.Count())
                DNISIndex = 0;
            int index = DNISIndex;
            ++DNISIndex;
            return DNIS[index];
        }

        string GetUSERID()
        {
            if (USERIDIndex >= USERID.Count())
                USERIDIndex = 0;
            int index = USERIDIndex;
            ++USERIDIndex;
            return USERID[index];
        }

        string GetWSIP()
        {
            if (WSIPIndex >= WSIP.Count())
                WSIPIndex = 0;
            int index = WSIPIndex;
            ++WSIPIndex;
            return WSIP[index];
        }

        string GetSipGWIP()
        {
            if (SipGWIPIndex >= SipGWIP.Count())
                SipGWIPIndex = 0;
            int index = SipGWIPIndex;
            ++SipGWIPIndex;
            return SipGWIP[index];
        }

        string GetSipSVRIP()
        {
            if (SipSVRIPIndex >= SipSVRIP.Count())
                SipSVRIPIndex = 0;
            int index = SipSVRIPIndex;
            ++SipSVRIPIndex;
            return SipSVRIP[index];
        }

        string GetSipSVRPort()
        {
            if (SipSVRPortIndex >= SipSVRPort.Count())
                SipSVRPortIndex = 0;
            int index = SipSVRPortIndex;
            ++SipSVRPortIndex;
            return SipSVRPort[index];
        }

        string GetSipSVRIPBackup()
        {
            if (SipSVRIPBackupIndex >= SipSVRIPBackup.Count())
                SipSVRIPBackupIndex = 0;
            int index = SipSVRIPBackupIndex;
            ++SipSVRIPBackupIndex;
            return SipSVRIPBackup[index];
        }

        string GetSipSVRPortBackup()
        {
            if (SipSVRPortBackupIndex >= SipSVRPortBackup.Count())
                SipSVRPortBackupIndex = 0;
            int index = SipSVRPortBackupIndex;
            ++SipSVRPortBackupIndex;
            return SipSVRPortBackup[index];
        }
        #endregion

        public bool MakeClickToDial()
        {
            var result = false;
            string url = "http://{0}:7717/inteliphonebook/webapi?action=create&ani={1}&dnis={2}&sipgwip={3}&sipsvrip={4}&sipsvrport={5}&sipsvripbackup={6}&sipsvrportbackup={7}&userid={8}";// +"&param=" + Math.random();

            string WSIPStr = GetWSIP();string ANIStr = GetANI();string DNISStr = GetDNIS();string SipGWIPStr = GetSipGWIP();
            string SipSVRIPStr = GetSipSVRIP(); string SipSVRPortStr = GetSipSVRPort(); string SipSVRIPBackupStr = GetSipSVRIPBackup();
            string SipSVRPortBackupStr = GetSipSVRPortBackup();string USERIDStr = GetUSERID();
            url = String.Format(url, WSIPStr, ANIStr, DNISStr, SipGWIPStr, SipSVRIPStr, SipSVRPortStr, SipSVRIPBackupStr, SipSVRPortBackupStr, USERIDStr);
            if (WSIPStr == "192.168.77.169") ++TotalConnectWSServerPredict;
            if (WSIPStr == "192.168.77.168") ++TotalCannotConnectWSServerPredict;
            /*if (bFSUp)
            {
                if ( SipGWIPStr == "192.168.77.168" )
            }
            else
            {
            }*/

            try
            {
                string sendResult = GetHtmlFromUrl(url);
                if (sendResult != null)
                {
                    log.Info("\r\n" + url + "\r\n" + sendResult + "\r\n");
                    if (sendResult.IndexOf("BUSY") >= 0) { }
                    else
                    {
                        int pos = sendResult.IndexOf("\r\n"); if (pos >= 0) sendResult = sendResult.Remove(pos);
                        ClickToDial addClickToDial = new ClickToDial();
                        addClickToDial.TaskID = sendResult;
                        addClickToDial.WSIP = WSIPStr;
                        taskList.Add(addClickToDial.TaskID, addClickToDial);
                    }
                    ++TotalConnectWSServer;
                    result = true;
                }
                else
                    ++TotalCannotConnectWSServer;
            }
            catch (Exception ex)
            {
                log.Info("\r\n" + url + "\r\nexception\r\n" + ex.Message + "\r\n");
                result = false;
            }
            return result;
        }

        public bool GetClickToDialStatus(ClickToDial p_checkClickToDial)
        {
            var result = false;
            string url = "http://{0}:7717/inteliphonebook/webapi?action=query&taskid={1}";

            url = String.Format(url, p_checkClickToDial.WSIP, p_checkClickToDial.TaskID);
            /*if (bFSUp)
            {
                if ( SipGWIPStr == "192.168.77.168" )
            }
            else
            {
            }*/

            try
            {
                string sendResult = GetHtmlFromUrl(url);
                log.Info("\r\n" + url + "\r\n" + sendResult + "\r\n");
                if (sendResult != null)
                {
                    if ( (sendResult.IndexOf("EXCEEDLIMIT") >= 0) || (sendResult.IndexOf("NOTFOUND") >= 0) )
                    {
                        taskIdList.Add(p_checkClickToDial.TaskID);
                    }
                    result = true;
                }

            }
            catch (Exception ex)
            {
                log.Info("\r\n" + url + "\r\nexception\r\n" + ex.Message + "\r\n");
                result = false;
            }
            return result;
        }

        public string GetHtmlFromUrl(string url)
        {
            string strRet = "";

            if (url == null || url.Trim().ToString() == "")
            {
                return strRet;
            }
            string targeturl = url.Trim().ToString();
            try
            {
                HttpWebRequest hr = (HttpWebRequest)WebRequest.Create(targeturl);
                hr.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)";
                hr.Method = "GET";
                hr.Timeout = 30 * 60 * 1000;
                WebResponse hs = hr.GetResponse();
                Stream sr = hs.GetResponseStream();
                StreamReader ser = new StreamReader(sr, System.Text.Encoding.UTF8);
                strRet = ser.ReadToEnd();
            }
            catch (Exception ex)
            {
                log.Info("\r\n" + url + "\r\nGetHtmlFromUrl exception\r\n" + ex.Message + "\r\n");
                strRet = null;
            }
            return strRet;
        }

        static public void DoWork(Object stateInfo)
        {
            DialWSTortureTest thdObj = (DialWSTortureTest)stateInfo;
            log.Info(String.Format("Thread ID:{0} starting.\r\n", thdObj.ID));
            thdObj.TotalSend = 0;
            thdObj.TotalConnectWSServer = 0;
            thdObj.TotalCannotConnectWSServer = 0;
            while (true)
            {
                if (IPBDialWSTortureTestForm.ServiceIsTerminating == 1)
                { Interlocked.Increment(ref IPBDialWSTortureTestForm.WSThreadTerminated); break; }
                ++thdObj.TotalSend;
                thdObj.MakeClickToDial();

                thdObj.taskIdList.Clear();
                foreach (ClickToDial checkClickToDial in thdObj.taskList.Values)
                {
                    thdObj.GetClickToDialStatus(checkClickToDial);
                }
                foreach (string taskid in thdObj.taskIdList)
                {
                    thdObj.taskList.Remove(taskid);
                    log.Info(String.Format("Thread ID:{0}.  Remove task:{1}.\r\n", thdObj.ID, taskid));
                }
                Thread.Sleep(10);
            }
            log.Info(String.Format("Thread ID:{0}.  Predict.  TotalSend:{1}.  ConnectWSServer Suc:{2}.  ConnectWSServer Fail:{3}.\r\n", thdObj.ID, thdObj.TotalSend, thdObj.TotalConnectWSServerPredict, thdObj.TotalCannotConnectWSServerPredict));
            log.Info(String.Format("Thread ID:{0}.  TotalSend:{1}.  ConnectWSServer Suc:{2}.  ConnectWSServer Fail:{3}.\r\n", thdObj.ID, thdObj.TotalSend, thdObj.TotalConnectWSServer, thdObj.TotalCannotConnectWSServer));
            log.Info(String.Format("Thread ID:{0}.  exited\r\n", thdObj.ID));
        }
    }
}
