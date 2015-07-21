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
        static public int FSIBThreadTerminated = 0;
        static public int FSOBThreadTerminated = 0;
        static public int HttpThreadTerminated = 0;
        static public int LicThreadTerminated = 0;
        #endregion

        private SMSProcessor SMSProcessor;
        private FSESOBProcessor FSESOBProcessor;
        private FSESIBProcessor FSESIBProcessor;
        private HttpProcessor HttpProcessor;
        private LicProcessor LicProcessor;

        static public List<SMSInfo> WaitingToSendSMSList = new List<SMSInfo>();
        static public Dictionary<string, ClickToDial> ClickToDialMap = new Dictionary<string, ClickToDial>();

        public InteliPhoneBookService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            log.Info("Starting...");
            SMSProcessor = new SMSProcessor();
            FSESOBProcessor = new FSESOBProcessor();
            FSESIBProcessor = new FSESIBProcessor();
            HttpProcessor = new HttpProcessor();
            LicProcessor = new LicProcessor();

            Thread Thread_sms = new Thread(new ParameterizedThreadStart(SMSProcessor.DoWork));
            Thread_sms.Start(SMSProcessor);
            Thread Thread_ob = new Thread(new ParameterizedThreadStart(FSESOBProcessor.DoWork));
            Thread_ob.Start(FSESOBProcessor);
            Thread Thread_ib = new Thread(new ParameterizedThreadStart(FSESIBProcessor.DoWork));
            Thread_ib.Start(FSESIBProcessor);
            Thread Thread_http = new Thread(new ParameterizedThreadStart(HttpProcessor.DoWork));
            Thread_http.Start(HttpProcessor);
            Thread Thread_lic = new Thread(new ParameterizedThreadStart(LicProcessor.DoWork));
            Thread_lic.Start(LicProcessor);
        }

        protected override void OnStop()
        {
            log.Info("Terminating...");
            Interlocked.Increment(ref ServiceIsTerminating);
            DateTime StartTerminating = DateTime.Now;
            while (true)
            {
                if (SMSThreadTerminated == 1 && FSIBThreadTerminated == 1 && FSOBThreadTerminated == 1 && HttpThreadTerminated == 1)
                {log.Info("Terminated, bye."); break;}
                else
                {
                    DateTime CurrentTime = DateTime.Now;
                    TimeSpan timeDiff = CurrentTime.Subtract(StartTerminating);
                    if (timeDiff.TotalSeconds > 5)
                    {
                        if (SMSThreadTerminated == 0)
                            log.Info("Something wrong happens in sms thread.");
                        if (FSIBThreadTerminated == 0)
                            log.Info("Something wrong happens in fs-es-ib thread.");
                        if (FSOBThreadTerminated == 0)
                            log.Info("Something wrong happens in fs-es-ob thread.");
                        if ( HttpThreadTerminated == 0)
                            log.Info("Something wrong happens in http thread.");
                        break;
                    }
                }
            }
        }
    }
}
