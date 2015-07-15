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

            for (int i = 0; i < 5; ++i)
            {
                DialWSTortureTest thdObj = new DialWSTortureTest();
                thdObj.ID = Convert.ToString(i + 1); thdObj.bFSUp = fsUp;
                /*ThreadPool.QueueUserWorkItem(new WaitCallback(DialWSTortureTest.DoWork), thdObj);*/

                Thread Thread = new Thread(new ParameterizedThreadStart(DialWSTortureTest.DoWork));
                Thread.Start(thdObj);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ServiceIsTerminating = 1;
        }
    }
}

