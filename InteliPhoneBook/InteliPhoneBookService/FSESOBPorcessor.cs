﻿//
//负责流程：呼叫助理。
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace InteliPhoneBookService
{
    class FSESOBProcessor
    {
        static public log4net.ILog log = log4net.LogManager.GetLogger("eslob");

        static public void DoWork(Object stateInfo)
        {
            FSESOBProcessor esobProcessor = (FSESOBProcessor)stateInfo;
            while (true)
            {
                if (InteliPhoneBookService.ServiceIsTerminating == 1)
                { Interlocked.Increment(ref InteliPhoneBookService.FSOBThreadTerminated); break; }
                Thread.Sleep(10);
            }
            log.Info("exited");
        }
    }
}
