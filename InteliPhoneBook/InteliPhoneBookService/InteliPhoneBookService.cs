using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using InteliPhoneBook.Model;

namespace InteliPhoneBookService
{
    public partial class InteliPhoneBookService : ServiceBase
    {
        static public log4net.ILog log = log4net.LogManager.GetLogger("root");
        #region /* thread sync variables*/
        static public int ServiceIsTerminating = 0;
        static public int SMSThreadTerminated = 0;
        static public int CurlThreadTerminated = 0;
        static public int FSOBThreadTerminated = 0;
        #endregion

        private SMSProcessor SMSProcessor;
        private FSESOBProcessor FSESOBProcessor;

        static public List<SMSInfo> WaitingToSendSMSList = new List<SMSInfo>();

        public InteliPhoneBookService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            log.Info("Starting...");
            SMSProcessor = new SMSProcessor();
            FSESOBProcessor = new FSESOBProcessor();
            ThreadPool.QueueUserWorkItem(new WaitCallback(SMSProcessor.DoWork), SMSProcessor);
            ThreadPool.QueueUserWorkItem(new WaitCallback(FSESOBProcessor.DoWork), FSESOBProcessor);
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
