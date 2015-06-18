using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MiniHttpd;

namespace InteliPhoneBookService
{
    class InteliPhoneBookHttpPage : IFile
    {
        private IDirectory parent = null;
        public delegate string HandleQuery(UriQuery query, string paramString);
        public delegate string HandleCreate(UriQuery query, string ani, string dnis, string sipgwip, string sipserverip, string sipserverport, string dialplanid);
        public event HandleCreate OnCreate;
        public event HandleQuery OnQueryStatus;
        public event HandleQuery OnCancel;

        public InteliPhoneBookHttpPage()
        {
        }
        public InteliPhoneBookHttpPage(IDirectory parent)
        {
            this.parent = parent;
        }

        /// <summary>
        /// Gets the name of the entry.
        /// </summary>
        public string Name
        {
            get { return "webapi"; }
        }

        /// <summary>
        /// Gets the parent directory of the object.
        /// </summary>
        public IDirectory Parent
        {
            get { return parent; }
        }

        /// <summary>
        /// Called when the file is requested by a client.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> requesting the file.</param>
        /// <param name="directory">The <see cref="IDirectory"/> of the parent directory.</param>
        public void OnFileRequested(HttpRequest request, IDirectory directory)
        {
            string taskid = request.Query.Get("taskid");
            string param = request.Query.Get("param");
            string action = request.Query.Get("action");
            string ani = request.Query.Get("ani");
            string dnis = request.Query.Get("dnis");
            string sipgwip = request.Query.Get("sipgwip");
            string sipsvrip = request.Query.Get("sipsvrip");
            string sipsvrport = request.Query.Get("sipsvrport");
            string dialplanid = request.Query.Get("dialplanid");

            if (action == null) { }
            else if (action == "create")
            {
                if (OnCreate != null)
                {
                    UriQuery queryString = new UriQuery(GetPostData(request));
                    string dialplan = OnCreate.Invoke(queryString, ani, dnis, sipgwip, sipsvrip, sipsvrport, dialplanid);
                    request.Response.BeginChunkedOutput();
                    System.IO.StreamWriter writer = new StreamWriter(request.Response.ResponseContent);
                    writer.Write(dialplan);
                    writer.Flush();
                    writer.Close();
                }
            }
            else if (action == "cancel")
            {
                if (OnCancel != null)
                {
                    UriQuery queryString = new UriQuery(GetPostData(request));
                    string dialplan = OnCancel.Invoke(queryString, taskid);
                    request.Response.BeginChunkedOutput();
                    System.IO.StreamWriter writer = new StreamWriter(request.Response.ResponseContent);
                    writer.Write(dialplan);
                    writer.Flush();
                    writer.Close();
                }
            }
            else if (action == "query")
            {
                if (OnQueryStatus != null)
                {
                    UriQuery queryString = new UriQuery(GetPostData(request));
                    string dialplan = OnQueryStatus.Invoke(queryString, taskid);
                    request.Response.BeginChunkedOutput();
                    System.IO.StreamWriter writer = new StreamWriter(request.Response.ResponseContent);
                    writer.Write(dialplan);
                    writer.Flush();
                    writer.Close();
                }
            }
            /*UriQuery queryString = new UriQuery(GetPostData(request));
            if (queryString["section"] == "dialplan")
            {
                if (OnGetDialplan != null)
                {
                    string dialplan = OnGetDialplan.Invoke(queryString);
                    request.Response.BeginChunkedOutput();
                    System.IO.StreamWriter writer = new StreamWriter(request.Response.ResponseContent);
                    writer.Write(dialplan);
                    writer.Flush();
                    writer.Close();
                }
            }
            else if (queryString["section"] == "directory")
            {
                if (OnGetUserDirectory != null)
                {
                    string userDirectory = OnGetUserDirectory.Invoke(queryString);
                    request.Response.BeginChunkedOutput();
                    System.IO.StreamWriter writer = new StreamWriter(request.Response.ResponseContent);
                    writer.Write(userDirectory);
                    writer.Flush();
                    writer.Close();
                }

            }*/
        }

        /// <summary>
        /// Gets the MIME type of the content.
        /// </summary>
        public string ContentType
        {
            get
            {
                return "text/xml";
            }
        }

        public void Dispose()
        {
        }

        void SetParent(IDirectory parent)
        {
            this.parent = parent;
        }


        private string GetPostData(HttpRequest request)
        {
            try
            {
                using (StreamReader reader = new StreamReader(request.PostData))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (ArgumentException err)
            {
                Console.WriteLine(err.ToString());
            }
            return "";
        }
    }
}