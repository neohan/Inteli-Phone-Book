//
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

                // We want curl to call http://xxxx:7717/freeswitch/curl.fetch
                VirtualDirectory dir = new VirtualDirectory("freeswitch", root);
                InteliPhoneBookHttpPage curlPage = new InteliPhoneBookHttpPage(dir);
                curlPage.OnGetDialplan += OnGetDialplan;
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

            protected string OnGetDialplan(UriQuery query)
            {
                //try{foreach (string key in query.AllKeys){;}}catch (Exception e){int i = 0;}

                return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>\r\n" +
                        "<document type=\"freeswitch/xml\">\r\n" +
                        "  <section name=\"dialplan\" description=\"RE Dial Plan For FreeSwitch\">\r\n" +
                        "    <context name=\"default\">\r\n" +
                        "      <extension name=\"test9\">\r\n" +
                        "        <condition field=\"destination_number\" expression=\"" + query["Caller-Destination-Number"] + "$\">\r\n" +
                        "        <action application=\"socket\" data=\"192.168.77.169:8022 async full\" />\r\n" +
                        "        </condition>\r\n" +
                        "      </extension>\r\n" +
                        "    </context>\r\n" +
                        "  </section>\r\n" +
                        "</document>\r\n";
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
            log.Info("exited");
        }
    }
}
