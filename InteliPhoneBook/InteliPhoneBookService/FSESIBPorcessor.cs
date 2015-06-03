﻿//
//负责流程：外呼桥接。
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Configuration;

namespace InteliPhoneBookService
{
    class FSESIBProcessor
    {
        static public readonly int ESL_SUCCESS = 1;
        static public log4net.ILog log = log4net.LogManager.GetLogger("eslib");

        static public FSESIBProcessor FSESIBProcessorObj = null;
        #region configuration data
        public int FSESLInboundModeServerPort = 0;                   //fs inbound mode server port
        public int FSESLInboundModeReconnectTimes = 0;                  //fs inbound mode reconnect times
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
                try { FSESLInboundModeServerPort = Int32.Parse(ConfigurationManager.AppSettings["FSESLInboundModeServerPort"]); }
                catch (Exception e) { FSESLInboundModeServerPort = 8021; } log.Info("FreeSWITCH ESL InboundMode Server Port:" + FSESLInboundModeServerPort);
                try { FSESLInboundModeReconnectTimes = Int32.Parse(ConfigurationManager.AppSettings["FSESLInboundModeReconnectTimes"]); }
                catch (Exception e) { FSESLInboundModeReconnectTimes = 3; } log.Info("FreeSWITCH ESL InboundMode Reconnect Times:" + FSESLInboundModeReconnectTimes);
            }
        }

        static public void DoWork(Object stateInfo)
        {
            FSESIBProcessor esibProcessor = (FSESIBProcessor)stateInfo;
            FSESIBProcessor.FSESIBProcessorObj = esibProcessor;
            esibProcessor.Initialize();
            while (true)
            {
                if (InteliPhoneBookService.ServiceIsTerminating == 1)
                { Interlocked.Increment(ref InteliPhoneBookService.FSIBThreadTerminated); break; }
                Thread.Sleep(1000);
            }
            log.Info("exited");
        }

        static public void ClickToDialDoWork(Object stateInfo)
        {
            InteliPhoneBook.Model.ClickToDial clickToDial = (InteliPhoneBook.Model.ClickToDial)stateInfo;
            log.Info(String.Format("task:{0} thread starting...\r\n", clickToDial.TaskID));
            int reconnectTimes = 0;
            string Ani = "4000", Dnis = "4001";
            string StateStr = "NONE", PrefixStr = "sofia/external/";
            while (true)
            {
                ESLconnection eslConnection = new ESLconnection(clickToDial.SIPGatewayIP, FSESIBProcessor.FSESIBProcessorObj.FSESLInboundModeServerPort, "ClueCon");
                if (eslConnection.Connected() != ESL_SUCCESS)
                {
                    log.Info(String.Format("task:{0}.Error connecting to FreeSwitch, do it again later.\r\n", clickToDial.TaskID));
                    ++reconnectTimes;
                    if (reconnectTimes > FSESIBProcessor.FSESIBProcessorObj.FSESLInboundModeReconnectTimes)
                    { log.Info(String.Format("task:{0} exceed limit\r\n", clickToDial.TaskID)); break; }
                    Thread.Sleep(1000); continue;
                }
                ESLevent eslEvent = eslConnection.SendRecv("event plain ALL");
                if (eslEvent == null)
                {
                    log.Info(String.Format("task:{0}.Error subscribing to all events.\r\n", clickToDial.TaskID));
                    ++reconnectTimes;
                    if (reconnectTimes > FSESIBProcessor.FSESIBProcessorObj.FSESLInboundModeReconnectTimes)
                    { log.Info(String.Format("task:{0} exceed limit\r\n", clickToDial.TaskID));break; }
                    Thread.Sleep(1000); continue;
                }
                eslEvent = eslConnection.Api("create_uuid", String.Empty);
                string originateUuid = eslEvent.GetBody();
                log.Info("create_uuid:" + originateUuid + "\r\n");
                eslConnection.Bgapi("originate", "{api_on_answer='uuid_hold " + originateUuid + "',origination_uuid=" + originateUuid + ",ignore_early_media=true,origination_caller_id_number=" + Dnis + "}sofia/external/" + Ani + "@" + clickToDial.SIPServerIP + ":" + clickToDial.SIPServerPort + " &bridge({api_on_ring='uuid_simplify " + originateUuid + "'}sofia/external/" + Dnis + "@" + clickToDial.SIPServerIP + ":" + clickToDial.SIPServerPort + ")", String.Empty);
                while (eslConnection.Connected() == ESL_SUCCESS)
                {
                    if (InteliPhoneBookService.ServiceIsTerminating == 1) break;
                    eslEvent = eslConnection.RecvEventTimed(10); if (eslEvent == null) { continue; }

                    string EventName = eslEvent.GetHeader("Event-Name", -1);
                    string UniqueUUID = eslEvent.GetHeader("Unique-ID", -1);
                    string ChannelName = eslEvent.GetHeader("Channel-Name", -1);
                    string ChannelState = eslEvent.GetHeader("Channel-State", -1);
                    string AnswerState = eslEvent.GetHeader("Answer-State", -1);
                    string ChannelCallUUID = eslEvent.GetHeader("Channel-Call-UUID", -1);
                    string OtherLegUniqueID = eslEvent.GetHeader("Other-Leg-Unique-ID", -1);
                    if (EventName != "MESSAGE_WAITING")
                        log.Info(eslEvent.Serialize(String.Empty) + "\r\n");
                    if (StateStr == "NONE")
                    {
                        if (EventName == "CHANNEL_OUTGOING" && UniqueUUID == originateUuid && ChannelName.Contains(PrefixStr + Ani + "@") == true)
                        {
                            StateStr = "INIT";
                            log.Info(String.Format("task:{0}  State going to INIT.\r\n", clickToDial.TaskID));
                        }
                    }
                    else if (StateStr == "INIT")
                    {
                        if (EventName == "CHANNEL_STATE" && UniqueUUID == originateUuid && ChannelState == "CS_INIT" && ChannelName.Contains(PrefixStr + Ani + "@") == true)
                        {
                            StateStr = "START";
                            log.Info(String.Format("task:{0}  State going to START.\r\n", clickToDial.TaskID));
                        }
                    }
                    else if (StateStr == "START")
                    {
                        if (EventName == "CHANNEL_CALLSTATE" && UniqueUUID == originateUuid && AnswerState == "answered" && ChannelName.Contains(PrefixStr + Ani + "@") == true)
                        {
                            StateStr = "ANIANS";
                            log.Info(String.Format("task:{0}  State going to ANIANS.\r\n", clickToDial.TaskID));
                        }
                    }
                    else if (StateStr == "ANIANS")
                    {
                        if (EventName == "CHANNEL_OUTGOING" && OtherLegUniqueID == originateUuid && ChannelName.Contains(PrefixStr + Dnis + "@") == true)
                        {
                            StateStr = "DNISINIT";
                            log.Info(String.Format("task:{0}  State going to DNISINIT.\r\n", clickToDial.TaskID));
                        }
                    }
                    else if (StateStr == "DNISINIT")
                    {
                        if (EventName == "CHANNEL_STATE" && ChannelCallUUID == originateUuid && ChannelState == "CS_INIT" && ChannelName.Contains(PrefixStr + Dnis + "@") == true)
                        {
                            StateStr = "DNISSTART";
                            log.Info(String.Format("task:{0}  State going to DNISSTART.\r\n", clickToDial.TaskID));
                        }
                    }
                    else if (StateStr == "DNISSTART")
                    {
                        if (EventName == "CHANNEL_CALLSTATE" && ChannelCallUUID == originateUuid && AnswerState == "answered" && ChannelName.Contains(PrefixStr + Dnis + "@") == true)
                        {
                            StateStr = "DNISANS";
                            log.Info(String.Format("task:{0}  State going to DNISANS.\r\n", clickToDial.TaskID));
                        }
                    }
                    else if (StateStr == "DNISANS")
                    {
                        if (EventName == "CHANNEL_BRIDGE" && UniqueUUID == originateUuid)
                        {
                            StateStr = "COMPLETE";
                            log.Info(String.Format("task:{0}  State going to COMPLETE.\r\n", clickToDial.TaskID));
                        }
                    }
                }
                break;
            }
            log.Info(String.Format("task:{0} thread exited\r\n", clickToDial.TaskID));
        }
    }
}