using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace IPBDialWSTortureTest
{
    public partial class IPBDialWSTortureTestForm : Form
    {
        #region /* thread sync variables*/
        static public int ServiceIsTerminating = 0;
        static public int WSThreadTerminated = 0;
        #endregion

        public int CreatedThread = 0;

        public IPBDialWSTortureTestForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool fsUp = false;
            if (checkBoxFSUp.Checked) fsUp = true;

            DialWSTortureTest thdObj = new DialWSTortureTest();
            thdObj.ID = "1"; thdObj.bFSUp = fsUp;
            ThreadPool.QueueUserWorkItem(new WaitCallback(DialWSTortureTest.DoWork), thdObj);

            DialWSTortureTest thdObj1 = new DialWSTortureTest();
            thdObj1.ID = "2"; thdObj1.bFSUp = fsUp;
            ThreadPool.QueueUserWorkItem(new WaitCallback(DialWSTortureTest.DoWork), thdObj1);

            DialWSTortureTest thdObj2 = new DialWSTortureTest();
            thdObj2.ID = "3"; thdObj2.bFSUp = fsUp;
            ThreadPool.QueueUserWorkItem(new WaitCallback(DialWSTortureTest.DoWork), thdObj2);

            DialWSTortureTest thdObj3 = new DialWSTortureTest();
            thdObj3.ID = "4"; thdObj3.bFSUp = fsUp;
            ThreadPool.QueueUserWorkItem(new WaitCallback(DialWSTortureTest.DoWork), thdObj3);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ServiceIsTerminating = 1;
        }
    }
}

