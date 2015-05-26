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
        public delegate string HandleQuery(UriQuery query);
        public event HandleQuery OnGetDialplan;
        public event HandleQuery OnGetUserDirectory;

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
            UriQuery queryString = new UriQuery(GetPostData(request));
            string dialplan = OnGetDialplan.Invoke(queryString);
            request.Response.BeginChunkedOutput();
            System.IO.StreamWriter writer = new StreamWriter(request.Response.ResponseContent);
            writer.Write(dialplan);
            writer.Flush();
            writer.Close();
            return;
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