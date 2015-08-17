﻿//
// 每次dynamic dialplan请求都必须检索数据库，如无法放问数据库，放一段缺省语音，然后挂断。
// 这样的缺省处理，要求每个FreeSWITCH实例安装时必须存在这样的语音文件。这样的处理利于检查问题。
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Configuration;
using MiniHttpd;

namespace InteliPhoneBookService
{
    class HttpProcessor
    {
        public static log4net.ILog log = log4net.LogManager.GetLogger("http");

        public int HttpPort;

        public class ModCurlHandler
        {
            private HttpWebServer server = null;

            public ModCurlHandler(HttpProcessor p_parent)
            {
                server = new HttpWebServer(p_parent.HttpPort);
                VirtualDirectory root = new VirtualDirectory("/", null);
                server.Root = root;

                // We want curl to call http://xxxx:7717/inteliphonebook/webapi
                VirtualDirectory dir = new VirtualDirectory("inteliphonebook", root);
                InteliPhoneBookHttpPage curlPage = new InteliPhoneBookHttpPage(dir);
                curlPage.OnCreate += OnCreate;
                curlPage.OnQueryStatus += OnQueryStatus;
                curlPage.OnCancel += OnCancel;
                dir.AddFile(curlPage);
                root.AddDirectory(dir);
            }

            public void Start()
            {
                server.Start();
            }

            public void Stop()
            {
                server.Stop();
            }

            protected string OnCreate(UriQuery query, string ani, string dnis, string sipgwip, string sipserverip, string sipserverport, string sipserveripbackup, string sipserverportbackup, string userid)
            {
                string taskId = "";
                lock (InteliPhoneBookService.ClickToDialMap)
                {
                    bool bFound;
                    while (true)
                    {//删除那些已经存在了很长时间的外拨对象。
                        bFound = false;
                        foreach (InteliPhoneBook.Model.ClickToDial deleteClickToDial in InteliPhoneBookService.ClickToDialMap.Values)
                        {
                            if (deleteClickToDial.CurrentStatus == "ANIBUSY" || deleteClickToDial.CurrentStatus == "ANINOANS" ||
                                deleteClickToDial.CurrentStatus == "ANIERR" || deleteClickToDial.CurrentStatus == "ANIINVALID" ||
                                deleteClickToDial.CurrentStatus == "COMPLETE" || deleteClickToDial.CurrentStatus == "EXCEEDLIMIT" || deleteClickToDial.CurrentStatus == "FINISH")
                            {
                                bFound = true; log.Info(String.Format("Remove Task:{0}.\r\n", deleteClickToDial.TaskID));
                                InteliPhoneBookService.ClickToDialMap.Remove(deleteClickToDial.TaskID); break;
                            }
                        }
                        if (bFound == false) break;
                    }
                    foreach (InteliPhoneBook.Model.ClickToDial checkClickToDial in InteliPhoneBookService.ClickToDialMap.Values)
                    {//检查是否存在针对同一个分机的外拨请求，如果存在则不生成此次外拨请求。
                        if (checkClickToDial.Ani == ani && checkClickToDial.SIPGatewayIP == sipgwip && checkClickToDial.SIPServerIP == sipserverip)
                        {
                            log.Info(String.Format("The same ani exist(TaskID:{0}), cannot create ClickToDial obj.\r\n", checkClickToDial.TaskID));
                            return "BUSY";
                        }
                    }

                    DateTime dtNow;
                    int intValue = 100;
                    string dateTimeStr = "";
                    bool bKeyExist = true;
                    while (bKeyExist)
                    {
                        ++intValue;
                        if (intValue > 999)
                            intValue = 100;
                        dtNow = DateTime.Now;
                        dateTimeStr = String.Format("{0:yyyyMMddHHmmssfff}", dtNow);
                        taskId = String.Format("{0}{1}", dateTimeStr, intValue);
                        bKeyExist = InteliPhoneBookService.ClickToDialMap.ContainsKey(taskId);
                        Thread.Sleep(1);
                    }
                    InteliPhoneBook.Model.ClickToDial clickToDial = new InteliPhoneBook.Model.ClickToDial();
                    clickToDial.CreateTime = DateTime.Now;
                    clickToDial.TaskID = taskId;
                    clickToDial.Ani = ani;
                    clickToDial.Dnis = dnis;
                    clickToDial.SIPGatewayIP = sipgwip;
                    clickToDial.SIPServerIP = sipserverip;
                    clickToDial.SIPServerPort = sipserverport;
                    if (String.IsNullOrEmpty(sipserverport))
                        clickToDial.SIPServerAddress = sipserverip;
                    else
                        clickToDial.SIPServerAddress = sipserverip + ":" + sipserverport;
                    clickToDial.SIPServerIPBackup = sipserveripbackup;
                    clickToDial.SIPServerPortBackup = sipserverportbackup;
                    if (String.IsNullOrEmpty(sipserveripbackup) == false)
                    {
                        if (String.IsNullOrEmpty(sipserverportbackup))
                            clickToDial.SIPServerAddressBackup = sipserveripbackup;
                        else
                            clickToDial.SIPServerAddressBackup = sipserveripbackup + ":" + sipserverportbackup;
                    }
                    clickToDial.UserID = userid;
                    InteliPhoneBookService.ClickToDialMap.Add(taskId, clickToDial);
                    log.Info(String.Format("TaskID:{0}, Ani:{1},  Dnis:{2}, GW:{3}, GWPort:{4}, SIPSvr:{5}, SIPPort:{6}.\r\n", clickToDial.TaskID, clickToDial.Ani, clickToDial.Dnis, clickToDial.SIPGatewayIP, clickToDial.SIPGatewayPort, clickToDial.SIPServerAddress, clickToDial.SIPServerAddressBackup));
                    //这个taskId返回给页面，后续调用其它查询请求，以此taskId为标识。
                    ThreadPool.QueueUserWorkItem(new WaitCallback(FSESIBProcessor.ClickToDialDoWork), clickToDial);
                }
                return taskId;
            }

            protected string OnQueryStatus(UriQuery query, string paramString)
            {
                string status = "NOTFOUND";
                lock (InteliPhoneBookService.ClickToDialMap)
                {
                    bool bKeyExist = InteliPhoneBookService.ClickToDialMap.ContainsKey(paramString);
                    if (bKeyExist)
                    {
                        InteliPhoneBook.Model.ClickToDial clickToDial = null;
                        InteliPhoneBookService.ClickToDialMap.TryGetValue(paramString, out clickToDial);
                        if (clickToDial != null)
                        {
                            clickToDial.ReturnStatus = clickToDial.CurrentStatus;
                            status = clickToDial.CurrentStatus;

                            if (status == "ANIBUSY" || status == "ANINOANS" ||
                                status == "ANIERR" || status == "ANIINVALID" ||
                                status == "COMPLETE" || status == "EXCEEDLIMIT" || status == "FINISH")
                            {
                                log.Info(String.Format("Remove Task:{0}.\r\n", clickToDial.TaskID));
                                InteliPhoneBookService.ClickToDialMap.Remove(clickToDial.TaskID);
                            }
                        }
                    }
                    return status;
                }
            }

            protected string OnCancel(UriQuery query, string paramString)
            {
                lock (InteliPhoneBookService.ClickToDialMap)
                {
                    bool bKeyExist = InteliPhoneBookService.ClickToDialMap.ContainsKey(paramString);
                    if (bKeyExist)
                    {
                        InteliPhoneBook.Model.ClickToDial clickToDial = null;
                        InteliPhoneBookService.ClickToDialMap.TryGetValue(paramString, out clickToDial);
                        ThreadPool.QueueUserWorkItem(new WaitCallback(FSESIBProcessor.CancelClickToDialDoWork), clickToDial);
                        return "";
                    }
                    else
                        return "NOTFOUND";
                }
            }
        }

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
                try { HttpPort = Int32.Parse(ConfigurationManager.AppSettings["HttpPort"]); }
                catch (Exception e) { HttpPort = 7717; }
            }

        }

        static public void DoWork(Object stateInfo)
        {
            HttpProcessor httpProcessor = (HttpProcessor)stateInfo;
            httpProcessor.Initialize();
            ModCurlHandler modCurlHandler = new ModCurlHandler(httpProcessor);
            modCurlHandler.Start();
            while (true)
            {
                if (InteliPhoneBookService.ServiceIsTerminating == 1)
                { Interlocked.Increment(ref InteliPhoneBookService.HttpThreadTerminated); break; }
                Thread.Sleep(10);
            }
            log.Info("exited\r\n");
        }
    }
}
