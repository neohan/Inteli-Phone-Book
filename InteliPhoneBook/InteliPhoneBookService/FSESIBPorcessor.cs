//
//负责流程：外呼桥接。
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace InteliPhoneBookService
{
    class FSESIBProcessor
    {
        static public log4net.ILog log = log4net.LogManager.GetLogger("eslib");

        static public void DoWork(Object stateInfo)
        {
            FSESIBProcessor esobProcessor = (FSESIBProcessor)stateInfo;
            while (true)
            {
                if (InteliPhoneBookService.ServiceIsTerminating == 1)
                { Interlocked.Increment(ref InteliPhoneBookService.FSIBThreadTerminated); break; }
                Thread.Sleep(10);
            }
            log.Info("exited");
        }
    }
}
