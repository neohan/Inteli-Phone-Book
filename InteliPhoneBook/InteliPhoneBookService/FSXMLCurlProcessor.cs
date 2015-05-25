//
// 每次dynamic dialplan请求都必须检索数据库，如无法放问数据库，放一段缺省语音，然后挂断。
// 这样的缺省处理，要求每个FreeSWITCH实例安装时必须存在这样的语音文件。这样的处理利于检查问题。
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace InteliPhoneBookService
{
    class FSXMLCurlProcessor
    {
        public static log4net.ILog log = log4net.LogManager.GetLogger("xcurl");

        static public void DoWork(Object stateInfo)
        {
            while (true)
            {
                if (InteliPhoneBookService.ServiceIsTerminating == 1)
                { Interlocked.Increment(ref InteliPhoneBookService.CurlThreadTerminated); break; }
                Thread.Sleep(10);
            }
            log.Info("exited");
        }
    }
}
