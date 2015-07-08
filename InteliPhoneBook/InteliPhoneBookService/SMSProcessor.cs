using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Net;
using System.IO;
using System.Configuration;
using InteliPhoneBook.Model;

namespace InteliPhoneBookService
{
    class SMSProcessor
    {
        static public log4net.ILog log = log4net.LogManager.GetLogger("sms");

        public int ReSendTimes = 0;              //重发短信次数
        private int ReSendAfterSeconds;          //重发短信间隔秒数
        public string EnterpriseCode;
        public string UserID;
        public string Pass;
        public string SignName;
        public string SmsTemplate;

        public bool Send(string mobile, string textMsg)
        {
            bool result = false;
            string auth = StringToMD5(EnterpriseCode + Pass, 32);
            string url = "http://210.5.158.31/hy?uid={0}&auth={1}&mobile={2}&msg={3}&expid=0";

            string content = textMsg;
            System.Text.Encoding encode = System.Text.Encoding.GetEncoding("GBK");
            content = HttpUtility.UrlEncode(content, encode);
            url = string.Format(url, UserID, auth, mobile, content);

            try
            {
                string sendResult = GetHtmlFromUrl(url);
                if (sendResult != null)
                {
                    log.Info("Send sms response:" + sendResult + ".  code:" + GetSMSResponseCodeDesc(sendResult) + "\r\n");
                    if (sendResult.IndexOf("0,") == 0 )
                        result = true;
                }
                else
                    log.Info("Send sms response is null\r\n");
            }
            catch (Exception ex) { result = false; log.Info("Send sms exception:" + ex.Message + "\r\n"); }

            return result;
        }

        public string StringToMD5(string str, int i)
        {
            //获取要加密的字段，并转化为Byte[]数组
            byte[] data = System.Text.Encoding.Unicode.GetBytes(str.ToCharArray());
            //建立加密服务
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            //加密Byte[]数组
            byte[] result = md5.ComputeHash(data);
            //将加密后的数组转化为字段
            if (i == 16 && str != string.Empty)
            {
                return System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(str, "MD5").ToLower().Substring(8, 16);
            }
            else if (i == 32 && str != string.Empty)
            {
                return System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(str, "MD5").ToLower();
            }
            else
            {
                switch (i)
                {
                    case 16: return "000000000000000";
                    case 32: return "000000000000000000000000000000";
                    default: return "请确保调用函数时第二个参数为16或32";
                }
            }
        }

        public string GetHtmlFromUrl(string url)
        {
            string strRet = null;

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
                StreamReader ser = new StreamReader(sr, System.Text.Encoding.Default);
                strRet = ser.ReadToEnd();
            }
            catch (Exception ex)
            {
                strRet = null;
            }
            return strRet;
        }

        public bool GetSMConfig()
        {
            bool bFound = false;
            StringBuilder strSQL = new StringBuilder();
            strSQL.Append("SELECT CompanyCode,LoginId,LoginPwd,SignName,SmsTemplate from SmsGateway WHERE Enabled = 1");
            log.Info(strSQL.ToString() + "\r\n");
            try
            {
                using (SqlDataReader rdr = SqlHelper.ExecuteReader(SqlHelper.SqlconnString, CommandType.Text, strSQL.ToString(), null))
                {
                    while (rdr.Read())
                    {
                        EnterpriseCode = rdr["CompanyCode"].ToString();
                        UserID = rdr["LoginId"].ToString();
                        Pass = rdr["LoginPwd"].ToString();
                        SignName = rdr["SignName"].ToString();
                        SmsTemplate = rdr["SmsTemplate"].ToString();
                        log.Info(String.Format("CompanyCode:{0}.  LoginId:{1}.  LoginPwd:{2}.  SignName:{3}.  SmsTemplate:{4}.  \r\n",
                                                rdr["CompanyCode"].ToString(), rdr["LoginId"].ToString(),
                                                rdr["LoginPwd"].ToString(), rdr["SignName"].ToString(), rdr["SmsTemplate"].ToString()));
                        bFound = true; break;
                    }
                }
            }
            catch (Exception e)
            {
                log.Info("Error occurs during GetSMConfig function.\r\n" + e.Message + "\r\n");
            }
            return bFound;
        }

        public void Initialize()
        {
            /*
             * 设置应该从数据库中获取,但如果连接数据库有问题,使用配置文件中的缺省参数。
             * 从SystemConfig表内取？
             */
            bool bConnectDBSuc = false;
            if (bConnectDBSuc == false)
            {
                try { ReSendTimes = Int32.Parse(ConfigurationManager.AppSettings["ReSendTimes"]); }
                catch (Exception e) { ReSendTimes = 3; }
                try { ReSendAfterSeconds = Int32.Parse(ConfigurationManager.AppSettings["ReSendAfterSeconds"]); }
                catch (Exception e) { ReSendAfterSeconds = 30; }
            }

            GetSMConfig();
        }

        public string GetSMSResponseCodeDesc(string p_response)
        {
            string temp;
            int pos = p_response.IndexOf(",");
            if (pos >= 0)
                temp = p_response.Remove(pos, p_response.Length - pos);
            else
                temp = p_response;
            switch (temp)
            {
                case "0":
                    return "操作成功";
                case "-1":
                    return "签权失败";
                case "-2":
                    return "未检索到被叫号码";
                case "-3":
                    return "被叫号码过多";
                case "-4":
                    return "内容未签名";
                case "-5":
                    return "内容过长";
                case "-6":
                    return "余额不足";
                case "-7":
                    return "暂停发送";
                case "-8":
                    return "保留";
                case "-9":
                    return "定时发送时间格式错误";
                case "-10":
                    return "下发内容为空";
                case "-11":
                    return "账户无效";
                case "-12":
                    return "IP地址非法";
                case "-13":
                    return "操作频率快";
                case "-14":
                    return "操作失败";
                case "-15":
                    return "拓展码无效";
                case "-16":
                    return "取消定时,seqid错误";
                case "-17":
                    return "未开通报告";
                case "-18":
                    return "暂留";
                case "-19":
                    return "未开通上行";
                case "-20":
                    return "暂留";
                case "-21":
                    return "包含屏蔽词";
                default:
                    return "未知";
            }
        }

        static public void DoWork(Object stateInfo)
        {
            SMSProcessor smsProcessor = (SMSProcessor)stateInfo;
            string logMsg;
            bool bCanSend;
            SMSInfo waitingToSend = null;

            smsProcessor.Initialize();
            while (true)
            {
                if (InteliPhoneBookService.ServiceIsTerminating == 1)
                { Interlocked.Increment(ref InteliPhoneBookService.SMSThreadTerminated); break; }
                Thread.Sleep(10);

                waitingToSend = null;
                lock (InteliPhoneBookService.WaitingToSendSMSList)
                {
                    foreach (SMSInfo smsInfo in InteliPhoneBookService.WaitingToSendSMSList)
                    {
                        bCanSend = false;
                        smsInfo.smmessage_ = smsProcessor.SmsTemplate;
                        smsInfo.smmessage_ = smsInfo.smmessage_.Replace("#ANI#", smsInfo.ani_).Replace("#DATETIME#", smsInfo.incommingtime_);
                        if (String.IsNullOrEmpty(smsInfo.callbackno_) )
                            smsInfo.smmessage_ = smsInfo.smmessage_.Replace("#CALLBACK#", smsInfo.ani_);
                        else
                            smsInfo.smmessage_ = smsInfo.smmessage_.Replace("#CALLBACK#", smsInfo.callbackno_);
                        logMsg = String.Format("waiting to send\r\n{0}, {1}, {2}, {3}, {4}, {5}", smsInfo.mobileno_, smsInfo.schedulesendtime_, smsInfo.ani_, smsInfo.dnis_, smsInfo.smmessage_, smsInfo.sendtimes_);

                        if (smsInfo.sendtimes_ == 0) { bCanSend = true; }
                        else
                        {
                            DateTime dtNow = DateTime.Now;
                            TimeSpan timeDiff = dtNow.Subtract(smsInfo.lastsendtime_);
                            if (timeDiff.TotalSeconds > smsProcessor.ReSendAfterSeconds)
                                bCanSend = true;
                        }
                        if (bCanSend == true)
                        {
                            waitingToSend = smsInfo;
                            InteliPhoneBookService.WaitingToSendSMSList.Remove(smsInfo);
                            logMsg += "\r\nsend now.";
                            log.Info(logMsg + "\r\n");
                            break;//找到了一个可以发送的短信记录就释放对列表的独占
                        }
                    }// end of foreach
                }//end of lock

                if (waitingToSend != null)
                {
                    if (smsProcessor.Send(waitingToSend.mobileno_, waitingToSend.smmessage_) == false)
                    {
                        logMsg = "\r\nsend fail.";
                        waitingToSend.sendtimes_ += 1;
                        if (waitingToSend.sendtimes_ >= smsProcessor.ReSendTimes)
                            logMsg += "exceed re-send limit.";
                        else
                        {
                            logMsg += "re-send later.";
                            waitingToSend.lastsendtime_ = DateTime.Now;
                            lock (InteliPhoneBookService.WaitingToSendSMSList)
                            {
                                InteliPhoneBookService.WaitingToSendSMSList.Add(waitingToSend);
                            }
                        }
                    }
                    else
                        logMsg = "send suc.";
                    log.Info(logMsg + "\r\n");
                    waitingToSend = null;//是否还需要存入数据库有待考虑。??????????
                }
            }//end of while
            log.Info("exited\r\n");
        }
    }
}