using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace InteliPhoneBookService
{
    public partial class InteliPhoneBookService : ServiceBase
    {
        public static log4net.ILog log = log4net.LogManager.GetLogger("root");
        public static int ServiceIsTerminating = 0;
        public static int SMSThreadTerminated = 0;
        public static int CurlThreadTerminated = 0;
        public static int FSOBThreadTerminated = 0;

        public InteliPhoneBookService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            log.Info("Starting...");
            ThreadPool.QueueUserWorkItem(new WaitCallback(SMSProcessor.DoWork));
            ThreadPool.QueueUserWorkItem(new WaitCallback(FSESOBPorcessor.DoWork));
            ThreadPool.QueueUserWorkItem(new WaitCallback(FSXMLCurlProcessor.DoWork));
        }

        protected override void OnStop()
        {
            log.Info("Terminating...");
            Interlocked.Increment(ref ServiceIsTerminating);
            DateTime StartTerminating = DateTime.Now;
            while (true)
            {
                if (SMSThreadTerminated == 1 && CurlThreadTerminated == 1 && FSOBThreadTerminated == 1)
                {log.Info("Terminated, bye."); break;}
                else
                {
                    DateTime CurrentTime = DateTime.Now;
                    TimeSpan timeDiff = CurrentTime.Subtract(StartTerminating);
                    if (timeDiff.TotalSeconds > 5)
                    {
                        if (SMSThreadTerminated == 0)
                            log.Info("Something wrong happens in sms thread.");
                        if (CurlThreadTerminated == 0)
                            log.Info("Something wrong happens in fs-xml-curl thread.");
                        if (FSOBThreadTerminated == 0)
                            log.Info("Something wrong happens in fs-es thread.");
                        break;
                    }
                }
            }
        }
    }
}
