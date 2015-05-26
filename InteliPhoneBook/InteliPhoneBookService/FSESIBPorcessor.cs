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

        #region configuration data
        public int FSESLInboundModeLocalPort = 0;                   //fs inbound mode local port
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
                try { FSESLInboundModeLocalPort = Int32.Parse(ConfigurationManager.AppSettings["FSESLInboundModeLocalPort"]); }
                catch (Exception e) { FSESLInboundModeLocalPort = 8022; } log.Info("FreeSWITCH ESL InboundMode Local Port:" + FSESLInboundModeLocalPort);
            }
        }

        static public void DoWork(Object stateInfo)
        {
            FSESIBProcessor esibProcessor = (FSESIBProcessor)stateInfo;
            esibProcessor.Initialize();
            while (true)
            {
                if (InteliPhoneBookService.ServiceIsTerminating == 1)
                { Interlocked.Increment(ref InteliPhoneBookService.FSIBThreadTerminated); break; }
                Thread.Sleep(10);

                ESLconnection eslConnection = new ESLconnection("192.168.77.168", esibProcessor.FSESLInboundModeLocalPort.ToString(), "ClueCon");
                if (eslConnection.Connected() != ESL_SUCCESS)
                { log.Info("Error connecting to FreeSwitch, do it again later."); Thread.Sleep(5000); continue; }
                ESLevent eslEvent = eslConnection.SendRecv("event plain ALL");
                if (eslEvent == null) { log.Info("Error subscribing to all events"); Thread.Sleep(5000); continue; }
                while (eslConnection.Connected() == ESL_SUCCESS)
                {
                    if (InteliPhoneBookService.ServiceIsTerminating == 1) { break; }
                    //查询外呼请求队列  发起外呼请求
                    eslEvent = eslConnection.RecvEventTimed(10); if (eslEvent == null) { continue; }
                }
                Thread.Sleep(1000);
            }
            log.Info("exited");
        }
    }
}
