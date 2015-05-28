//
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
            int reconnectTimes = 0;
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
                while (eslConnection.Connected() == ESL_SUCCESS)
                {
                    if (InteliPhoneBookService.ServiceIsTerminating == 1) break;
                    eslEvent = eslConnection.RecvEventTimed(10); if (eslEvent == null) { continue; }
                }
                break;
            }
            log.Info(String.Format("task:{0} thread exited\r\n", clickToDial.TaskID));
        }
    }
}