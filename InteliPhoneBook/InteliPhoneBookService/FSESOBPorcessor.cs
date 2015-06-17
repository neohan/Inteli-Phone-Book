//
//负责流程：呼叫助理。

//1，From是第一主叫
//2，To：是呼叫助理接入号
//3，Diversion by：是OXE上哪个分机转过来的

//如依据SIP消息中对端的IP地址以及接入号查不到任何信息，则不提供呼叫助理功能。

//根据每个用户的权限（是否开启短信通知），呼叫助理提供的语音提示是不相同的：
//一、未开启短信通知：对不起，您所呼叫的用户暂时不能应答，请稍候再拨。

//二、开启短信通知的语音：
//对不起，您所呼叫的用户暂时不能应答，如需短信通知对方，请按1。

//按1，选择短信通知后的语音：
//使用当前来电号码通知对方，请直接挂机。如需使用其他号码，请按1.

//按1，输入其他号码：
//请输入号码，输完按#号键。重新输入按*号键（按*号键重复此语音）。


//按#号键：
//信息已记录，再见。

//短信通知示例：
//您有一个未接来电来自：6700（姓名，若号码内有匹配），呼叫时间：2015-2-14 14:00.
//
using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Configuration;
using InteliPhoneBook.Model;

namespace InteliPhoneBookService
{
    class FSESOBProcessor
    {
        static public readonly int ESL_SUCCESS = 1;
        static public log4net.ILog log = log4net.LogManager.GetLogger("eslob");
        public static ManualResetEvent clientConnected = new ManualResetEvent(false);
        public enum CallAssistFlowState { 空闲=1, 无短信播放语音, 播放开始语音, 播放选择通知号码语音, 播放输入其他号码语音, 播放再见语音 }

        #region configuration data
        public int FSESLOutboundModeLocalPort = 0;              //fs outbound mode local port
        public int PlayWelcomeLimit = 0;                        //开始语音重播次数上限
        #endregion

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
                try { FSESLOutboundModeLocalPort = Int32.Parse(ConfigurationManager.AppSettings["FSESLOutboundModeLocalPort"]); }
                catch (Exception e) { FSESLOutboundModeLocalPort = 8022; } log.Info("FreeSWITCH ESL OutboundMode Local Port:" + FSESLOutboundModeLocalPort);
                try { PlayWelcomeLimit = Int32.Parse(ConfigurationManager.AppSettings["PlayWelcomeLimit"]); }
                catch (Exception e) { PlayWelcomeLimit = 3; } log.Info("Play Welcome Limit:" + PlayWelcomeLimit);
            }
        }

        public bool CallIsValid(string p_fromip, string p_fsip, string p_sipno)
        {
            bool bFound = false;
            StringBuilder strSQL = new StringBuilder();
            strSQL.Append("select VoiceHello from SipGateway, SipRelay, CallAssistant WHERE SipGateway.ID = SipRelay.GatewayId AND SipRelay.ID = CallAssistant.ID AND SipGateway.IPAddr = \'" + p_fsip + "\' AND SipRelay.IPAddr = \'" + p_fromip + "\' AND CallAssistant.Assistant = \'" + p_sipno + "\'");
            log.Info(strSQL.ToString());
            try
            {
                using (SqlDataReader rdr = SqlHelper.ExecuteReader(SqlHelper.SqlconnString, CommandType.Text, strSQL.ToString(), null))
                {
                    while (rdr.Read())
                    {
                        log.Info(String.Format("VoiceHello:{0}\r\n", rdr["VoiceHello"].ToString()));
                        bFound = true;
                    }
                    if ( bFound == true )
                        log.Info(String.Format("Call is valid.from ip:{0},fs ip:{1},sipno:{2}\r\n", p_fromip, p_fsip, p_sipno));
                    else
                        log.Info(String.Format("Call is invalid.from ip:{0},fs ip:{1},sipno:{2}\r\n", p_fromip, p_fsip, p_sipno));
                }
            }
            catch (Exception e)
            {
                log.Info("Error occurs during CallIsValid function.\r\n" + e.Message);
            }
            return bFound;
        }

        public bool SMSNotifyEnable(string p_ani, string p_dnis, string p_sipno, out string p_tel)
        {
            p_tel = "";
            StringBuilder strSQL = new StringBuilder();
            strSQL.Append("select SMSNotify,TelSJ from UserInfo WHERE TelBG = \'" + p_dnis + "\'");
            try
            {
                using (SqlDataReader rdr = SqlHelper.ExecuteReader(SqlHelper.SqlconnString, CommandType.Text, strSQL.ToString(), null))
                {
                    while (rdr.Read())
                    {
                        log.Info(String.Format("SMSNotify:{0},according to:{1}\r\n", rdr["SMSNotify"].ToString(), p_dnis));
                        if (rdr["SMSNotify"].ToString() == "1")
                        {
                            p_tel = rdr["TelSJ"].ToString();
                            return true;
                        }
                        else
                            return false;
                    }
                    log.Info(String.Format("Cannot find any info.ani:{0},dnis:{1},sipno:{2}\r\n", p_ani, p_dnis, p_sipno));
                }
            }
            catch (Exception e)
            {
                log.Info("Error occurs during SMSNotifyEnable function.\r\n" + e.Message);
            }
            return false;
        }

        public string GetCallerName(string p_ani)
        {
            StringBuilder strSQL = new StringBuilder();
            strSQL.Append("select Name from UserInfo WHERE TelBG = \'" + p_ani + "\'");
            try
            {
                using (SqlDataReader rdr = SqlHelper.ExecuteReader(SqlHelper.SqlconnString, CommandType.Text, strSQL.ToString(), null))
                {
                    while (rdr.Read())
                    {
                        log.Info(String.Format("Name:{0},according to:{1}\r\n", rdr["Name"].ToString(), p_ani));
                        return rdr["Name"].ToString();
                    }
                    log.Info(String.Format("Cannot find name by ani:{0}\r\n", p_ani));
                }
            }
            catch (Exception e)
            {
                log.Info("Error occurs during GetCallerName function.\r\n" + e.Message);
            }
            return "";
        }

        static public void DoWork(Object stateInfo)
        {
            FSESOBProcessor esobProcessor = (FSESOBProcessor)stateInfo;
            esobProcessor.Initialize();
            while (true)
            {
                if (InteliPhoneBookService.ServiceIsTerminating == 1)
                { Interlocked.Increment(ref InteliPhoneBookService.FSOBThreadTerminated); break; }
                Thread.Sleep(10);

                /* add next line to a dialplan
                 <action application="socket" data="localhost:8022 async full" /> */
                IPAddress ipAddress = Dns.Resolve("localhost").AddressList[0];
                TcpListener tcpListener;

                log.Info("Listening on port:" + esobProcessor.FSESLOutboundModeLocalPort);
                try { tcpListener =  new TcpListener(IPAddress.Parse("192.168.77.169"), esobProcessor.FSESLOutboundModeLocalPort); }
                catch ( Exception e){ log.Error("new TcpListener error. do it again later.\r\n" + e.ToString()); Thread.Sleep(5000); continue;}


                try
                {
                    tcpListener.Start();

                    log.Info("OutboundModeAsync, waiting for connections...");

                    while (true)
                    {//这个异步接收客户端连接的底层多线程模型是怎样的，需要详细考察下。
                        tcpListener.BeginAcceptSocket((asyncCallback) =>
                        {
                            TcpListener tcpListened = (TcpListener)asyncCallback.AsyncState;

                            Socket sckClient = tcpListened.EndAcceptSocket(asyncCallback);
                            log.Info("Set ManualResetEvent object."); clientConnected.Set();

                            //Initializes a new instance of ESLconnection, and connects to the host $host on the port $port, and supplies $password to freeswitch
                            ESLconnection eslConnection = new ESLconnection(sckClient.Handle.ToInt32());

                            ESLevent eslEvent = eslConnection.GetInfo();
                            string sip_from_host = eslEvent.GetHeader("variable_sip_from_host", -1);
                            string fs_host = eslEvent.GetHeader("variable_sip_req_host", -1);
                            string strUuid = eslEvent.GetHeader("UNIQUE-ID", -1);
                            string user_entered_keys = "";
                            string sip_to_user = "", sip_from_user = "", sip_req_user = "", notify_tel_no = "";
                            DateTime incomming_time = DateTime.Now;
                            Int32 playWelcomeCount = 0, playSMSChoiceCount = 0, playEnterOtherPhoneNo = 0;
                            CallAssistFlowState callAssistFlowState = CallAssistFlowState.空闲;
                            bool bCanSendSMS = false, bCallbackNoIsCurrent = false; log.Info(eslEvent.Serialize(String.Empty));
                            sip_req_user = eslEvent.GetHeader("Caller-Destination-Number", -1);
                            if (esobProcessor.CallIsValid(sip_from_host, fs_host, sip_req_user) == false)
                                eslConnection.Disconnect();
                            else
                            {
                                //log.Info(eslEvent.Serialize(String.Empty));

                                eslConnection.SendRecv("myevents");
                                eslConnection.SendRecv("divert_events on");
                                //流程启动前还需检索接入号对应的语音文件。存储在数据库内，依据sip_req_user,以及IP地址。
                                //
                                eslConnection.Execute("answer", String.Empty, String.Empty);
                                while (eslConnection.Connected() == ESL_SUCCESS)
                                {
                                    eslEvent = eslConnection.RecvEventTimed(10); if (eslEvent == null) { continue; } log.Info(eslEvent.Serialize(String.Empty));
                                    strUuid = eslEvent.GetHeader("UNIQUE-ID", -1);
                                    string event_name = eslEvent.GetHeader("Event-Name", -1);
                                    string appname = eslEvent.GetHeader("Application", -1);
                                    if (event_name == "CHANNEL_EXECUTE")
                                    {
                                        if (eslEvent.GetHeader("Channel-Call-State", -1) == "RINGING")
                                        {
                                            sip_from_user = eslEvent.GetHeader("variable_sip_from_user", -1);
                                            sip_to_user = eslEvent.GetHeader("variable_sip_to_user", -1);
                                            sip_req_user = eslEvent.GetHeader("variable_sip_req_user", -1);
                                            log.Info(String.Format("Incomming Call  CallAssitFlow  UNIQUE-ID:{0},  Ani:{1},  Dnis:{2},  SIP Trunk No:{3}\r\n", strUuid, sip_from_user, sip_to_user, sip_req_user));
                                        }
                                    }
                                    else if (event_name == "CHANNEL_EXECUTE_COMPLETE")
                                    {
                                        string app_response = eslEvent.GetHeader("Application-Response", -1);

                                        if (appname == "answer")
                                        {//查询数据库原始被叫号码是否开启了短信通知功能
                                            //语音文件名由sipno决定
                                            if (esobProcessor.SMSNotifyEnable(sip_from_user, sip_to_user, sip_req_user, out notify_tel_no) == false)
                                            {
                                                callAssistFlowState = CallAssistFlowState.无短信播放语音;
                                                eslConnection.Execute("playback", "welcome-no.wav", String.Empty);
                                                log.Info(String.Format("UNIQUE-ID:{0}  Event-Name:CHANNEL_EXECUTE_COMPLETE   Application-Response:answer\r\nFlow State:{1}\r\n", strUuid, CallAssistFlowState.无短信播放语音.ToString()));
                                            }
                                            else
                                            {
                                                ++playWelcomeCount;
                                                callAssistFlowState = CallAssistFlowState.播放开始语音;
                                                eslConnection.Execute("playback", "welcome.wav", String.Empty);
                                                log.Info(String.Format("UNIQUE-ID:{0}  Event-Name:CHANNEL_EXECUTE_COMPLETE   Application-Response:answer\r\nFlow State:{1}  Times:{2}\r\nNotify tel no:{3}\r\n", strUuid, CallAssistFlowState.播放开始语音.ToString(), playWelcomeCount, notify_tel_no));
                                            }
                                        }
                                        if (appname == "play_and_get_digits")
                                        {
                                            if (callAssistFlowState == CallAssistFlowState.播放输入其他号码语音)
                                            {
                                                callAssistFlowState = CallAssistFlowState.播放再见语音;
                                                user_entered_keys = eslEvent.GetHeader("variable_inteliphonebook-other-phoneno", -1);
                                                log.Info(String.Format("UNIQUE-ID:{0}  Event-Name:CHANNEL_EXECUTE_COMPLETE   Application:play_and_get_digits\r\nFlow State:{1}  Digits:{2}  Go to the final step.\r\n", strUuid, CallAssistFlowState.播放输入其他号码语音.ToString(), user_entered_keys));
                                                log.Info(eslEvent.Serialize(String.Empty));
                                                eslConnection.Execute("playback", "bye.wav", String.Empty);
                                                bCanSendSMS = true;
                                                bCallbackNoIsCurrent = false;
                                            }
                                        }
                                        if (app_response == "FILE PLAYED")
                                        {
                                            if (callAssistFlowState == CallAssistFlowState.无短信播放语音)
                                            {
                                                callAssistFlowState = CallAssistFlowState.空闲;
                                                eslConnection.Execute("hangup", String.Empty, String.Empty);
                                                log.Info(String.Format("UNIQUE-ID:{0}  Event-Name:CHANNEL_EXECUTE_COMPLETE   Application-Response:FILE PLAYED\r\nFlow State:{1}\r\n", strUuid, CallAssistFlowState.无短信播放语音.ToString()));
                                            }
                                            else if (callAssistFlowState == CallAssistFlowState.播放开始语音)
                                            {
                                                if (playWelcomeCount >= 3)
                                                {
                                                    playWelcomeCount = 0;
                                                    callAssistFlowState = CallAssistFlowState.空闲;
                                                    eslConnection.Execute("hangup", String.Empty, String.Empty);
                                                    log.Info(String.Format("UNIQUE-ID:{0}  Event-Name:CHANNEL_EXECUTE_COMPLETE   Application-Response:FILE PLAYED\r\nFlow State:{1}  Times:{2}  Exceed limit, hangup.\r\n", strUuid, CallAssistFlowState.播放开始语音.ToString(), playWelcomeCount));
                                                }
                                                else
                                                {
                                                    ++playWelcomeCount;
                                                    callAssistFlowState = CallAssistFlowState.播放开始语音;
                                                    eslConnection.Execute("playback", "welcome.wav", String.Empty);
                                                    log.Info(String.Format("UNIQUE-ID:{0}  Event-Name:CHANNEL_EXECUTE_COMPLETE   Application-Response:FILE PLAYED\r\nFlow State:{1}  Times:{2}  play again.\r\n", strUuid, CallAssistFlowState.播放开始语音.ToString(), playWelcomeCount));
                                                }
                                            }
                                            else if (callAssistFlowState == CallAssistFlowState.播放选择通知号码语音)
                                            {
                                                if (playSMSChoiceCount >= 3)
                                                {
                                                    playSMSChoiceCount = 0;
                                                    //记录本机号码 播放了三次这个语音相当于客户选择使用当前呼入主叫作为回电号码。
                                                    callAssistFlowState = CallAssistFlowState.空闲;
                                                    eslConnection.Execute("hangup", String.Empty, String.Empty);
                                                    log.Info(String.Format("UNIQUE-ID:{0}  Event-Name:CHANNEL_EXECUTE_COMPLETE   Application-Response:FILE PLAYED\r\nFlow State:{1}  Times:{2}  Exceed limit, hangup.\r\n", strUuid, CallAssistFlowState.播放选择通知号码语音.ToString(), playSMSChoiceCount));
                                                    bCallbackNoIsCurrent = true; bCanSendSMS = true;
                                                }
                                                else
                                                {
                                                    ++playSMSChoiceCount;
                                                    eslConnection.Execute("playback", "callback-current.wav", String.Empty);
                                                    log.Info(String.Format("UNIQUE-ID:{0}  Event-Name:CHANNEL_EXECUTE_COMPLETE   Application-Response:FILE PLAYED\r\nFlow State:{1}  Times:{2}.\r\n", strUuid, CallAssistFlowState.播放选择通知号码语音.ToString(), playSMSChoiceCount));
                                                }
                                            }
                                            else if (callAssistFlowState == CallAssistFlowState.播放输入其他号码语音)
                                            {
                                                if (playEnterOtherPhoneNo >= 3)
                                                {
                                                    playEnterOtherPhoneNo = 0;
                                                    //记录本机号码 播放了三次这个语音相当于客户选择使用当前呼入主叫作为回电号码。
                                                    callAssistFlowState = CallAssistFlowState.空闲;
                                                    eslConnection.Execute("hangup", String.Empty, String.Empty);
                                                    log.Info(String.Format("UNIQUE-ID:{0}  Event-Name:CHANNEL_EXECUTE_COMPLETE   Application-Response:FILE PLAYED\r\nFlow State:{1}  Times:{2}  Exceed limit, hangup.\r\n", strUuid, CallAssistFlowState.播放输入其他号码语音.ToString(), playEnterOtherPhoneNo));
                                                    bCallbackNoIsCurrent = true;
                                                }
                                                else
                                                {
                                                    ++playEnterOtherPhoneNo;
                                                    eslConnection.Execute("playback", "enter-other-phoneno.wav", String.Empty);
                                                    log.Info(String.Format("UNIQUE-ID:{0}  Event-Name:CHANNEL_EXECUTE_COMPLETE   Application-Response:FILE PLAYED\r\nFlow State:{1}  Times:{2}.\r\n", strUuid, CallAssistFlowState.播放输入其他号码语音.ToString(), playEnterOtherPhoneNo));
                                                }
                                            }
                                            else if (callAssistFlowState == CallAssistFlowState.播放再见语音)
                                            {
                                                callAssistFlowState = CallAssistFlowState.空闲;
                                                eslConnection.Execute("hangup", String.Empty, String.Empty);
                                            }
                                        }

                                    }
                                    else if (event_name == "DTMF")
                                    {
                                        string dtmf_digit = eslEvent.GetHeader("DTMF-Digit", -1);
                                        if (callAssistFlowState == CallAssistFlowState.播放开始语音)
                                        {
                                            if (dtmf_digit == "1")
                                            {
                                                playWelcomeCount = 0;
                                                ++playSMSChoiceCount;
                                                callAssistFlowState = CallAssistFlowState.播放选择通知号码语音;
                                                bCallbackNoIsCurrent = true; bCanSendSMS = true;
                                                eslConnection.Execute("playback", "callback-current.wav", String.Empty);
                                                log.Info(String.Format("UNIQUE-ID:{0}  Event-Name:DTMF   DTMF-Digit:1\r\nFlow State:{1}  Times:{2}\r\n", strUuid, CallAssistFlowState.播放选择通知号码语音.ToString(), playSMSChoiceCount));
                                            }
                                        }
                                        else if (callAssistFlowState == CallAssistFlowState.播放选择通知号码语音)
                                        {
                                            if (dtmf_digit == "1")
                                            {
                                                bCanSendSMS = false; bCallbackNoIsCurrent = false;
                                                playSMSChoiceCount = 0;
                                                user_entered_keys = ""; playEnterOtherPhoneNo = 0; ++playEnterOtherPhoneNo;
                                                callAssistFlowState = CallAssistFlowState.播放输入其他号码语音;
                                                eslConnection.Execute("play_and_get_digits", "1 11 3 10000 # enter-other-phoneno.wav enter-other-phoneno.wav inteliphonebook-other-phoneno .+ 4000", String.Empty);
                                                log.Info(String.Format("UNIQUE-ID:{0}  Event-Name:DTMF   DTMF-Digit:1\r\nFlow State:{1}  Times:{2}\r\n", strUuid, CallAssistFlowState.播放输入其他号码语音.ToString(), playEnterOtherPhoneNo));
                                            }
                                        }
                                        else if (callAssistFlowState == CallAssistFlowState.播放输入其他号码语音)
                                        {
                                            if (dtmf_digit == "*")
                                            {
                                                user_entered_keys = ""; playEnterOtherPhoneNo = 0; ++playEnterOtherPhoneNo;
                                                callAssistFlowState = CallAssistFlowState.播放输入其他号码语音;
                                                eslConnection.Execute("play_and_get_digits", "1 11 3 10000 # enter-other-phoneno.wav enter-other-phoneno.wav inteliphonebook-other-phoneno .+ 4000", String.Empty);
                                                log.Info(String.Format("UNIQUE-ID:{0}  Event-Name:DTMF   DTMF-Digit:*\r\nFlow State:{1}  Times:{2}\r\n",
                                                                        strUuid,
                                                                        CallAssistFlowState.播放输入其他号码语音.ToString(),
                                                                        playEnterOtherPhoneNo));
                                            }
                                        }
                                    }
                                }

                                log.Info(String.Format("Connection closed. UNIQUE-ID:{0}", strUuid));
                                if (bCanSendSMS)
                                {
                                    if (String.IsNullOrEmpty(notify_tel_no))
                                        log.Info(String.Format("UNIQUE-ID:{0}  Notify tel no is null,cannot create sm object.", strUuid));
                                    else
                                    {
                                        SMSInfo smsInfo = new SMSInfo();
                                        string name = esobProcessor.GetCallerName(sip_from_user);
                                        smsInfo.mobileno_ = notify_tel_no;
                                        if (string.IsNullOrEmpty(name)) ;
                                        else
                                            name = "(" + name + ")";
                                        if (bCallbackNoIsCurrent)
                                            smsInfo.smmessage_ = String.Format("您有一个未接来电来自：{0}{1}，呼叫时间：{2}", sip_from_user, name, incomming_time);
                                        else
                                            smsInfo.smmessage_ = String.Format("您有一个未接来电来自：{0}{1}，呼叫时间：{2}", user_entered_keys, name, incomming_time);
                                        InteliPhoneBookService.WaitingToSendSMSList.Add(smsInfo);
                                    }
                                }
                            }
                        }, tcpListener);


                        TimeSpan timeSpan = new TimeSpan(100 * 100);
                        while (clientConnected.WaitOne(timeSpan) == false)
                        {
                            if (InteliPhoneBookService.ServiceIsTerminating == 1) break;
                        }
                        if (InteliPhoneBookService.ServiceIsTerminating == 1) break;
                        log.Info("ManualResetEvent object is reset, Begin Accept Socket again.");
                        clientConnected.Reset();
                        Thread.Sleep(50);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    tcpListener.Stop();
                }
            }
            log.Info("exited");
        }
    }
}


//语音文件播放完后,等待一定时间接收客户按键
//客户按键后,等待一定时间才算超时,此时重新播放语音或者结束