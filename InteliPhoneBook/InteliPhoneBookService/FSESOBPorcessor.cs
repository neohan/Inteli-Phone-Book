//
//负责流程：呼叫助理。

//1，From是第一主叫
//2，To：是呼叫助理接入号
//3，Diversion by：是OXE上哪个分机转过来的


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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Configuration;

namespace InteliPhoneBookService
{
    class FSESOBProcessor
    {
        static public readonly int ESL_SUCCESS = 1;
        static public log4net.ILog log = log4net.LogManager.GetLogger("eslob");
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
                catch (Exception e) { FSESLOutboundModeLocalPort = 8022; }
                try { PlayWelcomeLimit = Int32.Parse(ConfigurationManager.AppSettings["PlayWelcomeLimit"]); }
                catch (Exception e) { PlayWelcomeLimit = 3; }
            }
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

                try { tcpListener =  new TcpListener(ipAddress, esobProcessor.FSESLOutboundModeLocalPort); }
                catch ( Exception e){ log.Error("new TcpListener error. do it again later.\r\n" + e.ToString()); Thread.Sleep(5000); continue;}


                try
                {
                    tcpListener.Start();

                    log.Info("OutboundModeAsync, waiting for connections...");

                    while (true)
                    {
                        tcpListener.BeginAcceptSocket((asyncCallback) =>
                        {
                            TcpListener tcpListened = (TcpListener)asyncCallback.AsyncState;

                            Socket sckClient = tcpListened.EndAcceptSocket(asyncCallback);

                            //Initializes a new instance of ESLconnection, and connects to the host $host on the port $port, and supplies $password to freeswitch
                            ESLconnection eslConnection = new ESLconnection(sckClient.Handle.ToInt32());

                            ESLevent eslEvent = eslConnection.GetInfo();
                            string strUuid = eslEvent.GetHeader("UNIQUE-ID", -1);
                            string user_entered_keys = "";
                            string flow_state = "play_welcome";
                            string sip_ip = "";
                            string sip_port = "";
                            string sip_from_user = "";
                            DateTime play_done_or_key_pressed_time = DateTime.Now;
                            Int32 playWelcomeCount = 0, playSMSChoiceCount = 0, playEnterOtherPhoneNo = 0;
                            CallAssistFlowState callAssistFlowState = CallAssistFlowState.空闲;

                            eslConnection.SendRecv("myevents");
                            eslConnection.SendRecv("divert_events on");

                            eslConnection.Execute("answer", String.Empty, String.Empty);
                            while (eslConnection.Connected() == ESL_SUCCESS)
                            {
                                eslEvent = eslConnection.RecvEventTimed(10); if (eslEvent == null) { continue; }
                                //log.Info(eslEvent.Serialize(String.Empty));
                                string name = eslEvent.GetHeader("Event-Name", -1);
                                Console.WriteLine("Event-Name:" + name + "\r\n");
                                string appname = eslEvent.GetHeader("Application", -1);
                                Console.WriteLine("Application:" + appname + "\r\n\r\n");
                                if (name == "CHANNEL_EXECUTE")
                                {
                                    if (eslEvent.GetHeader("Channel-Call-State", -1) == "RINGING")
                                    {
                                        sip_ip = eslEvent.GetHeader("variable_sip_network_ip", -1);
                                        sip_port = eslEvent.GetHeader("variable_sip_network_port", -1);
                                        sip_from_user = eslEvent.GetHeader("variable_sip_from_user", -1);
                                        Console.WriteLine("RINGRING   Caller-Network-Addr:" + eslEvent.GetHeader("Caller-Network-Addr", -1) + "\r\n");
                                        Console.WriteLine("RINGRING   Caller-Destination-Number:" + eslEvent.GetHeader("Caller-Destination-Number", -1) + "\r\n");
                                    }
                                }
                                else if (name == "CHANNEL_EXECUTE_COMPLETE")
                                {
                                    string app_response = eslEvent.GetHeader("Application-Response", -1);

                                    if (appname == "answer")
                                    {//查询数据库决定执行哪个流程
                                        if (1 == 1)
                                        {
                                            callAssistFlowState = CallAssistFlowState.无短信播放语音;
                                            eslConnection.Execute("playback", "std_welcome.wav", String.Empty);
                                        }
                                        else
                                        {
                                            ++playWelcomeCount;
                                            callAssistFlowState = CallAssistFlowState.播放开始语音;
                                            eslConnection.Execute("play_and_get_digits", "11 11 3 10000 # gbestsmbl/gbestsmbl-enter_notifyed_phoneno.wav gbestsmbl/gbestsmbl-enter_again.wav .+", String.Empty);
                                        }
                                    }
                                    if (app_response == "FILE PLAYED")
                                    {
                                        if (callAssistFlowState == CallAssistFlowState.无短信播放语音)
                                        {
                                            callAssistFlowState = CallAssistFlowState.空闲;
                                            eslConnection.Execute("hangup", String.Empty, String.Empty);
                                        }
                                        else if (callAssistFlowState == CallAssistFlowState.播放开始语音)
                                        {
                                            if (playWelcomeCount >= 3)
                                            {
                                                callAssistFlowState = CallAssistFlowState.空闲;
                                                eslConnection.Execute("hangup", String.Empty, String.Empty);
                                            }
                                            else
                                            {
                                                ++playWelcomeCount;
                                                callAssistFlowState = CallAssistFlowState.播放开始语音;
                                                eslConnection.Execute("play_and_get_digits", "11 11 3 10000 # gbestsmbl/gbestsmbl-enter_notifyed_phoneno.wav gbestsmbl/gbestsmbl-enter_again.wav .+", String.Empty);
                                            }
                                        }
                                        else if (callAssistFlowState == CallAssistFlowState.播放选择通知号码语音)
                                        {
                                            if (playSMSChoiceCount >= 3)
                                            {
                                                //记录本机号码
                                                callAssistFlowState = CallAssistFlowState.空闲;
                                                eslConnection.Execute("hangup", String.Empty, String.Empty);
                                            }
                                            else
                                            {
                                                ++playSMSChoiceCount;
                                                callAssistFlowState = CallAssistFlowState.播放开始语音;
                                                eslConnection.Execute("play_and_get_digits", "11 11 3 10000 # gbestsmbl/gbestsmbl-enter_notifyed_phoneno.wav gbestsmbl/gbestsmbl-enter_again.wav .+", String.Empty);
                                            }
                                        }
                                        else if (callAssistFlowState == CallAssistFlowState.播放再见语音)
                                        {
                                            callAssistFlowState = CallAssistFlowState.空闲;
                                            eslConnection.Execute("hangup", String.Empty, String.Empty);
                                        }
                                    }

                                }
                                else if (name == "DTMF")
                                {
                                    string dtmf_digit = eslEvent.GetHeader("DTMF-Digit", -1);
                                    if (callAssistFlowState == CallAssistFlowState.播放开始语音)
                                    {
                                        if (dtmf_digit == "1")
                                        {
                                            playWelcomeCount = 0;
                                            ++playSMSChoiceCount;
                                            callAssistFlowState = CallAssistFlowState.播放选择通知号码语音;
                                            eslConnection.Execute("play_and_get_digits", "11 11 3 10000 # gbestsmbl/gbestsmbl-enter_notifyed_phoneno.wav gbestsmbl/gbestsmbl-enter_again.wav .+", String.Empty);
                                        }
                                        else
                                        {
                                            if (playWelcomeCount >= 3)
                                            {
                                                callAssistFlowState = CallAssistFlowState.空闲;
                                                eslConnection.Execute("hangup", String.Empty, String.Empty);
                                            }
                                            else
                                            {
                                                ++playWelcomeCount;
                                                eslConnection.Execute("play_and_get_digits", "11 11 3 10000 # gbestsmbl/gbestsmbl-enter_notifyed_phoneno.wav gbestsmbl/gbestsmbl-enter_again.wav .+", String.Empty);
                                            }
                                        }
                                    }
                                    else if (callAssistFlowState == CallAssistFlowState.播放选择通知号码语音)
                                    {
                                        if (dtmf_digit == "1")
                                        {
                                            playSMSChoiceCount = 0;
                                            ++playEnterOtherPhoneNo;
                                            callAssistFlowState = CallAssistFlowState.播放输入其他号码语音;
                                            eslConnection.Execute("play_and_get_digits", "11 11 3 10000 # gbestsmbl/gbestsmbl-enter_notifyed_phoneno.wav gbestsmbl/gbestsmbl-enter_again.wav .+", String.Empty);
                                        }
                                        else
                                        {
                                            if (playSMSChoiceCount >= 3)
                                            {//记录本机号码
                                                callAssistFlowState = CallAssistFlowState.空闲;
                                                eslConnection.Execute("hangup", String.Empty, String.Empty);
                                            }
                                            else
                                            {
                                                ++playSMSChoiceCount;
                                                eslConnection.Execute("play_and_get_digits", "11 11 3 10000 # gbestsmbl/gbestsmbl-enter_notifyed_phoneno.wav gbestsmbl/gbestsmbl-enter_again.wav .+", String.Empty);
                                            }
                                        }
                                    }
                                    else if (callAssistFlowState == CallAssistFlowState.播放输入其他号码语音)
                                    {
                                        if (dtmf_digit == "1")
                                        {
                                            callAssistFlowState = CallAssistFlowState.播放再见语音;
                                            eslConnection.Execute("play_and_get_digits", "11 11 3 10000 # gbestsmbl/gbestsmbl-enter_notifyed_phoneno.wav gbestsmbl/gbestsmbl-enter_again.wav .+", String.Empty);
                                        }
                                        else
                                        {
                                            if (playEnterOtherPhoneNo >= 3)
                                            {//记录本机号码
                                                callAssistFlowState = CallAssistFlowState.空闲;
                                                eslConnection.Execute("hangup", String.Empty, String.Empty);
                                            }
                                            else
                                            {
                                                ++playEnterOtherPhoneNo;
                                                eslConnection.Execute("play_and_get_digits", "11 11 3 10000 # gbestsmbl/gbestsmbl-enter_notifyed_phoneno.wav gbestsmbl/gbestsmbl-enter_again.wav .+", String.Empty);
                                            }
                                        }
                                    }
                                }
                            }

                            sckClient.Close();
                            Console.WriteLine("Connection closed uuid:{0}", strUuid);

                        }, tcpListener);

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