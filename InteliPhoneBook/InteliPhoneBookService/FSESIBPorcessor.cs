//
//负责流程：外呼桥接。

//用户是否具备外线拨打权限不在此处处理，所以提交给此程序的外呼请求均认定为具备拨打权限。
//
using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Configuration;
using System.Text.RegularExpressions;

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
        public int FSESLInboundModeRecreateUUIDTimes = 0;
        public int FSESLInboundModeAniAnsTimeout = 0;
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
                catch (Exception e) { FSESLInboundModeReconnectTimes = 3; } log.Info("FreeSWITCH ESL InboundMode Reconnect Limit:" + FSESLInboundModeReconnectTimes);
                try { FSESLInboundModeRecreateUUIDTimes = Int32.Parse(ConfigurationManager.AppSettings["FSESLInboundModeRecreateUUIDTimes"]); }
                catch (Exception e) { FSESLInboundModeRecreateUUIDTimes = 3; } log.Info("FreeSWITCH ESL InboundMode Re-create UUID Limit:" + FSESLInboundModeRecreateUUIDTimes);
                try {FSESLInboundModeAniAnsTimeout = Int32.Parse(ConfigurationManager.AppSettings["FSESLInboundModeAniAnsTimeout"]);}
                catch (Exception e) { FSESLInboundModeAniAnsTimeout = 60; } log.Info("FreeSWITCH ESL InboundMode Ani Answer Timeout(s):" + FSESLInboundModeAniAnsTimeout);
            }
        }

        public List<InteliPhoneBook.Model.DialRule> GetDialRules(string p_dialplanid)
        {
            List<InteliPhoneBook.Model.DialRule> dialRules = new List<InteliPhoneBook.Model.DialRule>();
            StringBuilder strSQL = new StringBuilder();
            strSQL.Append("select Number,DeleteLen,AddCrown from DialRules WHERE PlanID = \'" + p_dialplanid + "\' ORDER BY DisplayOrder");
            log.Info(strSQL.ToString());
            try
            {
                using (SqlDataReader rdr = SqlHelper.ExecuteReader(SqlHelper.SqlconnString, CommandType.Text, strSQL.ToString(), null))
                {
                    while (rdr.Read())
                    {
                        InteliPhoneBook.Model.DialRule dialRule = new InteliPhoneBook.Model.DialRule();
                        dialRule.PhoneNumber = rdr["Number"].ToString();
                        try { dialRule.DeleteLen = Convert.ToInt32(rdr["DeleteLen"].ToString()); }catch (Exception e) { dialRule.DeleteLen = -1; }
                        dialRule.PrefixToAdd = rdr["AddCrown"].ToString();
                        dialRules.Add(dialRule);
                        log.Info(String.Format("Number:{0}.  DeleteLen:{1}.  PrefixToAdd:{2}.  according to:{3}\r\n", dialRule.PhoneNumber, dialRule.DeleteLen, dialRule.PrefixToAdd, p_dialplanid));
                    }
                    return dialRules;
                }
            }
            catch (Exception e)
            {
                log.Info(String.Format("Error occurs during GetDialRules function.  Dial Plan ID:{0}\r\n{1}", p_dialplanid, e.Message));
            }
            return null;
        }

        private string ReplaceQuestionMark(string p_originalString)
        {
            return p_originalString.Replace("?", "\\d");
        }

        private string ReplaceBarBreak(string p_originalString)
        {
            return p_originalString.Replace("-", "\\d+");
        }

        public string ApplyDialRules(string p_originalDnis, List<InteliPhoneBook.Model.DialRule> p_dialrules)
        {
            if (p_dialrules == null || p_dialrules.Count == 0)
                return p_originalDnis;
            string phoneno = p_originalDnis;
            foreach (InteliPhoneBook.Model.DialRule dialrule in p_dialrules)
            {
                if ( dialrule.DeleteLen == -1 ) continue;
                string resultRegex;
                resultRegex = ReplaceQuestionMark(dialrule.PhoneNumber);
                resultRegex = ReplaceBarBreak(resultRegex);
                resultRegex = "^" + resultRegex;
                Regex dialRulsRegex = new Regex(resultRegex);
                if (dialRulsRegex.IsMatch(p_originalDnis))
                {
                    phoneno = phoneno.Remove(0, dialrule.DeleteLen);
                    phoneno = dialrule.PrefixToAdd + phoneno;
                    break;
                }
            }
            return phoneno;
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
            log.Info(String.Format("task:{0} thread starting...\r\n", clickToDial.TaskID));
            int reconnectTimes = 0;
            string Ani = clickToDial.Ani, Dnis = clickToDial.Dnis;
            string StateStr = "NONE", PrefixStr = "sofia/external/", dialedNo;
            while (true)
            {//如果连不上fs也要求记录一下状态,利于诊断问题,这些信息同样也需要在界面上显示。
                ESLconnection eslConnection = new ESLconnection(clickToDial.SIPGatewayIP, FSESIBProcessor.FSESIBProcessorObj.FSESLInboundModeServerPort, "ClueCon");
                if (eslConnection.Connected() != ESL_SUCCESS)
                {
                    clickToDial.CurrentStatus = "CONNECTING";
                    log.Info(String.Format("task:{0}.Error connecting to FreeSwitch, do it again later.\r\n", clickToDial.TaskID));
                    ++reconnectTimes;
                    if (reconnectTimes > FSESIBProcessor.FSESIBProcessorObj.FSESLInboundModeReconnectTimes)
                    { log.Info(String.Format("task:{0} exceed limit\r\n", clickToDial.TaskID)); clickToDial.CurrentStatus = "EXCEEDLIMIT"; break; }
                    Thread.Sleep(1000); continue;
                }
                ESLevent eslEvent = eslConnection.SendRecv("event plain ALL");
                if (eslEvent == null)
                {
                    clickToDial.CurrentStatus = "CONNECTED";
                    log.Info(String.Format("task:{0}.Error subscribing to all events.\r\n", clickToDial.TaskID));
                    ++reconnectTimes;
                    if (reconnectTimes > FSESIBProcessor.FSESIBProcessorObj.FSESLInboundModeReconnectTimes)
                    { log.Info(String.Format("task:{0} exceed limit\r\n", clickToDial.TaskID)); clickToDial.CurrentStatus = "EXCEEDLIMIT"; break; }
                    Thread.Sleep(1000); continue;
                }
                //发起呼叫前，检索拨号计划，进行相应处理。
                List<InteliPhoneBook.Model.DialRule> dialrules = FSESIBProcessor.FSESIBProcessorObj.GetDialRules(clickToDial.DialPlanID);
                Dnis = FSESIBProcessor.FSESIBProcessorObj.ApplyDialRules(clickToDial.Dnis, dialrules);
                int recreateTimes = 0;
                do
                {
                    ++recreateTimes;
                    eslEvent = eslConnection.Api("create_uuid", String.Empty);
                    if (eslEvent == null)
                    {
                        clickToDial.CurrentStatus = "PROCESSING";
                        log.Info(String.Format("task:{0}.Error create_uuid.\r\n", clickToDial.TaskID));
                        if (recreateTimes > FSESIBProcessor.FSESIBProcessorObj.FSESLInboundModeRecreateUUIDTimes)
                        { log.Info(String.Format("task:{0} re-create UUID exceed limit\r\n", clickToDial.TaskID)); break; }
                        Thread.Sleep(500);
                    }
                }
                while (eslEvent == null);
                if (eslEvent == null) continue;
                string originateUuid = eslEvent.GetBody();
                log.Info("create_uuid:" + originateUuid + "\r\n");
                clickToDial.Uuid = originateUuid;
                eslConnection.Bgapi("originate", "{originate_timeout=" + FSESIBProcessor.FSESIBProcessorObj.FSESLInboundModeAniAnsTimeout + ",api_on_answer='uuid_hold " + originateUuid + "',origination_uuid=" + originateUuid + ",ignore_early_media=true,origination_caller_id_number=" + Dnis + "}sofia/external/" + Ani + "@" + clickToDial.SIPServerIP + ":" + clickToDial.SIPServerPort + " &bridge({api_on_ring='uuid_simplify " + originateUuid + "'}sofia/external/" + Dnis + "@" + clickToDial.SIPServerIP + ":" + clickToDial.SIPServerPort + ")", String.Empty);
                while (eslConnection.Connected() == ESL_SUCCESS)
                {
                    if (InteliPhoneBookService.ServiceIsTerminating == 1) break;
                    eslEvent = eslConnection.RecvEventTimed(10); if (eslEvent == null) { continue; }

                    string EventName = eslEvent.GetHeader("Event-Name", -1);
                    string UniqueUUID = eslEvent.GetHeader("Unique-ID", -1);
                    string ChannelName = eslEvent.GetHeader("Channel-Name", -1);
                    string ChannelState = eslEvent.GetHeader("Channel-State", -1);
                    string AnswerState = eslEvent.GetHeader("Answer-State", -1);
                    string ChannelCallUUID = eslEvent.GetHeader("Channel-Call-UUID", -1);
                    string OtherLegUniqueID = eslEvent.GetHeader("Other-Leg-Unique-ID", -1);
                    string HangupCause = eslEvent.GetHeader("Hangup-Cause", -1);
                    if ((EventName != "MESSAGE_WAITING")&&(EventName != "MESSAGE_QUERY"))
                        log.Info(eslEvent.Serialize(String.Empty) + "\r\n");

                    if (EventName == "BACKGROUND_JOB")
                    {
                        string eventBody = eslEvent.GetBody();
                        if (eventBody.Contains("-ERR"))
                        {
                            if (StateStr == "NONE" || StateStr == "INIT" || StateStr == "START" || StateStr == "ANIRINGING")
                            {
                                if (eventBody.Contains(" USER_BUSY"))
                                {
                                    StateStr = "ANIBUSY"; clickToDial.CurrentStatus = "ANIBUSY";
                                }
                                else if (eventBody.Contains(" NO_ANSWER") || eventBody.Contains(" PROGRESS_TIMEOUT"))
                                {
                                    StateStr = "ANINOANS"; clickToDial.CurrentStatus = "ANINOANS";
                                }
                                else if (eventBody.Contains(" UNALLOCATED_NUMBER") )
                                {
                                    StateStr = "ANIINVALID"; clickToDial.CurrentStatus = "ANIINVALID";
                                }
                                else
                                {
                                    StateStr = "ANIERR"; clickToDial.CurrentStatus = "ANIERR";
                                }
                                log.Info(String.Format("task:{0} finished.  State going to {1}.\r\n", clickToDial.TaskID, StateStr));
                                break;
                            }
                            else if (StateStr == "DNISINIT" || StateStr == "DNISSTART")
                            {
                                if (eventBody.Contains(" USER_BUSY"))
                                {
                                    StateStr = "DNISBUSY"; clickToDial.CurrentStatus = "DNISBUSY";
                                }
                                else if (eventBody.Contains(" NO_ANSWER"))
                                {
                                    StateStr = "DNISNOANS"; clickToDial.CurrentStatus = "DNISNOANS";
                                }
                                else
                                {
                                    StateStr = "DNISERR"; clickToDial.CurrentStatus = "DNISERR";
                                }
                                log.Info(String.Format("task:{0} finished.  State going to {1}.\r\n", clickToDial.TaskID, StateStr));
                                break;
                            }
                        }
                    }
                    if (StateStr == "NONE")
                    {
                        if (EventName == "CHANNEL_OUTGOING" && UniqueUUID == originateUuid && ChannelName.Contains(PrefixStr + Ani + "@") == true)
                        {
                            StateStr = "INIT"; clickToDial.CurrentStatus = "INIT";
                            log.Info(String.Format("task:{0}  State going to INIT.\r\n", clickToDial.TaskID));
                        }
                    }
                    else if (StateStr == "INIT")
                    {
                        if (EventName == "CHANNEL_STATE" && UniqueUUID == originateUuid && ChannelState == "CS_INIT" && ChannelName.Contains(PrefixStr + Ani + "@") == true)
                        {
                            StateStr = "START"; clickToDial.CurrentStatus = "START";
                            log.Info(String.Format("task:{0}  State going to START.\r\n", clickToDial.TaskID));
                        }
                    }
                    else if (StateStr == "START")
                    {
                        if (EventName == "CHANNEL_CALLSTATE" && ChannelCallUUID == originateUuid && AnswerState == "ringing" && ChannelName.Contains(PrefixStr + Ani + "@") == true)
                        {
                            StateStr = "ANIRINGING"; clickToDial.CurrentStatus = "ANIRINGING";
                            log.Info(String.Format("task:{0}  State going to ANIRINGING.\r\n", clickToDial.TaskID));
                        }
                    }
                    else if (StateStr == "ANIRINGING")
                    {
                        if (EventName == "CHANNEL_CALLSTATE" && UniqueUUID == originateUuid && AnswerState == "answered" && ChannelName.Contains(PrefixStr + Ani + "@") == true)
                        {
                            StateStr = "ANIANS"; clickToDial.CurrentStatus = "ANIANS";
                            log.Info(String.Format("task:{0}  State going to ANIANS.\r\n", clickToDial.TaskID));
                        }
                    }
                    else if (StateStr == "ANIANS")
                    {
                        if (EventName == "CHANNEL_OUTGOING" && OtherLegUniqueID == originateUuid && ChannelName.Contains(PrefixStr + Dnis + "@") == true)
                        {
                            StateStr = "DNISINIT"; clickToDial.CurrentStatus = "DNISINIT";
                            log.Info(String.Format("task:{0}  State going to DNISINIT.\r\n", clickToDial.TaskID));
                        }
                    }
                    else if (StateStr == "DNISINIT")
                    {
                        if (EventName == "CHANNEL_STATE" && ChannelCallUUID == originateUuid && ChannelState == "CS_INIT" && ChannelName.Contains(PrefixStr + Dnis + "@") == true)
                        {
                            StateStr = "DNISSTART"; clickToDial.CurrentStatus = "DNISSTART";
                            log.Info(String.Format("task:{0}  State going to DNISSTART.\r\n", clickToDial.TaskID));
                        }
                    }
                    else if (StateStr == "DNISSTART")
                    {
                        if (EventName == "CHANNEL_CALLSTATE" && ChannelCallUUID == originateUuid && AnswerState == "ringing" && ChannelName.Contains(PrefixStr + Dnis + "@") == true)
                        {
                            StateStr = "DNISRINGING"; clickToDial.CurrentStatus = "DNISRINGING";
                            log.Info(String.Format("task:{0}  State going to DNISRINGING.\r\n", clickToDial.TaskID));
                        }
                        else if (EventName == "CHANNEL_CALLSTATE" && ChannelCallUUID == originateUuid && AnswerState == "hangup" && ChannelName.Contains(PrefixStr + Dnis + "@") == true
                            && HangupCause == "NO_USER_RESPONSE" )
                        {
                            StateStr = "DNISNORESP"; clickToDial.CurrentStatus = "DNISNORESP";
                            log.Info(String.Format("task:{0} finished  State going to DNISNORESP.\r\n", clickToDial.TaskID));
                            break;
                        }
                        else if (EventName == "CHANNEL_CALLSTATE" && ChannelCallUUID == originateUuid && AnswerState == "hangup" && ChannelName.Contains(PrefixStr + Dnis + "@") == true
                            && HangupCause == "UNALLOCATED_NUMBER")
                        {
                            StateStr = "DNISINVALID"; clickToDial.CurrentStatus = "DNISINVALID";
                            log.Info(String.Format("task:{0} finished  State going to DNISINVALID.\r\n", clickToDial.TaskID));
                            break;
                        }
                        else if (EventName == "CHANNEL_CALLSTATE" && ChannelCallUUID == originateUuid && AnswerState == "hangup" && ChannelName.Contains(PrefixStr + Dnis + "@") == true
                            && HangupCause == "NORMAL_TEMPORARY_FAILURE")
                        {
                            StateStr = "DNISTEMPFAIL"; clickToDial.CurrentStatus = "DNISTEMPFAIL";
                            log.Info(String.Format("task:{0} finished  State going to DNISTEMPFAIL.\r\n", clickToDial.TaskID));
                            break;
                        }
                        else if (EventName == "CHANNEL_CALLSTATE" && ChannelCallUUID == originateUuid && AnswerState == "hangup" && ChannelName.Contains(PrefixStr + Dnis + "@") == true)
                        {
                            StateStr = "DNISFATAL" + HangupCause; clickToDial.CurrentStatus = "DNISFATAL" + HangupCause;
                            log.Info(String.Format("task:{0} finished  State going to DNISFATAL{1}.\r\n", clickToDial.TaskID, HangupCause));
                            break;
                        }
                    }
                    else if (StateStr == "DNISRINGING")
                    {
                        if (EventName == "CHANNEL_STATE" && HangupCause == "BLIND_TRANSFER" && UniqueUUID == originateUuid)
                        {
                            StateStr = "COMPLETE"; clickToDial.CurrentStatus = "COMPLETE";
                            log.Info(String.Format("task:{0}  State going to COMPLETE.\r\n", clickToDial.TaskID));
                            break;
                        }
                    }
                }
                eslConnection.Disconnect();
                break;
            }
            log.Info(String.Format("task:{0} thread exited\r\n", clickToDial.TaskID));
        }

        static public void CancelClickToDialDoWork(Object stateInfo)
        {
            InteliPhoneBook.Model.ClickToDial clickToDial = (InteliPhoneBook.Model.ClickToDial)stateInfo;
            log.Info(String.Format("cancel task:{0} thread starting...\r\n", clickToDial.TaskID));
            int reconnectTimes = 0;
            while (true)
            {
                ESLconnection eslConnection = new ESLconnection(clickToDial.SIPGatewayIP, FSESIBProcessor.FSESIBProcessorObj.FSESLInboundModeServerPort, "ClueCon");
                if (eslConnection.Connected() != ESL_SUCCESS)
                {
                    log.Info(String.Format("cancel task:{0}.Error connecting to FreeSwitch, do it again later.\r\n", clickToDial.TaskID));
                    ++reconnectTimes;
                    if (reconnectTimes > FSESIBProcessor.FSESIBProcessorObj.FSESLInboundModeReconnectTimes)
                    { log.Info(String.Format("cancel task:{0} exceed limit\r\n", clickToDial.TaskID)); break; }
                    Thread.Sleep(1000); continue;
                }
                ESLevent eslEvent = eslConnection.Api("uuid_kill", clickToDial.Uuid);
                log.Info(eslEvent.Serialize(String.Empty) + "\r\n");
                break;
            }

            log.Info(String.Format("cancel task:{0} thread exited\r\n", clickToDial.TaskID));
        }
    }
}