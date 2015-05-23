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
        }
    }
}
