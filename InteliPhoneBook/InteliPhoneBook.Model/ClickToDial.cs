using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InteliPhoneBook.Model
{
    public class ClickToDial
    {
        public string TaskID;
        public string SIPGatewayIP;
        public string SIPGatewayPort;
        public string SIPServerIP;
        public string SIPServerPort;
        public string Ani;
        public string Dnis;
        public string Uuid;
        public string UserID;

        public DateTime CreateTime;                 //记录创建时间，是为了在超时时长后丢弃此实例。
        public string CurrentStatus;
    }
}
