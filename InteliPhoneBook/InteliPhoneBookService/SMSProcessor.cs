using System;
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

        public bool Send(string mobile, string textMsg)
        {
            var result = false;
            string enterpriseCode = "daet";
            string uid = "50467";
            string pass = "daet01";
            string auth = StringToMD5(enterpriseCode + pass, 32);
            string url = "http://210.5.158.31/hy?uid={0}&auth={1}&mobile={2}&msg={3}&expid=0";

            string content = textMsg;
            System.Text.Encoding encode = System.Text.Encoding.GetEncoding("GBK");
            content = HttpUtility.UrlEncode(content, encode);
            url = string.Format(url, uid, auth, mobile, content);

            try
            {
                var sendResult = GetHtmlFromUrl(url);
                if (sendResult != null)
                {
                    log.Info("Send sms response:" + sendResult);
                    result = true;
                }
            }
            catch (Exception ex)
            {
                result = false;
            }
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

        public void Initialize()
        {
            /*
             * 设置应该从数据库中获取,但如果连接数据库有问题,使用配置文件中的缺省参数。
             * 从SystemConfig表内取？
             */
            bool bConnectDBSuc = false;
            
            //这里放查询数据库代码。

            if (bConnectDBSuc == false)
            {
                try { ReSendTimes = Int32.Parse(ConfigurationManager.AppSettings["ReSendTimes"]); }
                catch (Exception e) { ReSendTimes = 3; }
                try { ReSendAfterSeconds = Int32.Parse(ConfigurationManager.AppSettings["ReSendAfterSeconds"]); }
                catch (Exception e) { ReSendAfterSeconds = 30; }
            }
        }

        static public void DoWork(Object stateInfo)
        {
            SMSProcessor smsProcessor = (SMSProcessor)stateInfo;
            string logMsg;
            bool bCanSend;
            SMSInfo waitingToSend = null;

            /*SMSInfo smsInfoa = new SMSInfo();
            smsInfoa.ani_ = "139983";
            smsInfoa.mobileno_ = "13916394304";
            smsInfoa.smmessage_ = "这是一条测试信息。【企福惠】";
            InteliPhoneBookService.WaitingToSendSMSList.Add(smsInfoa);*/

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
                            log.Info(logMsg);
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
                            logMsg = "exceed re-send limit.";
                        else
                        {
                            logMsg = "re-send later.";
                            waitingToSend.lastsendtime_ = DateTime.Now;
                            lock (InteliPhoneBookService.WaitingToSendSMSList)
                            {
                                InteliPhoneBookService.WaitingToSendSMSList.Add(waitingToSend);
                            }
                        }
                    }
                    else
                        logMsg = "\r\nsend suc.";
                    log.Info(logMsg);
                    waitingToSend = null;//是否还需要存入数据库有待考虑。??????????
                }
            }//end of while
            log.Info("exited");
        }
    }
}