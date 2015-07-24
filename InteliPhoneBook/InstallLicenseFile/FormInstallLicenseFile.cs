using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Security.Cryptography;

/*
 * 不只是将许可文件安装到指定目录，在拷贝前还必须检查许可是否合法，等于将核心程序使用检查许可的方法再拷贝一份到这个许可安装程序内。
 * 如果目标目录内已存在一个合法的许可文件，还需提示是否需要替换。
 */

namespace InstallLicenseFile
{
    public partial class FormInstallLicenseFile : Form
    {
        private static byte[] ProjectInsideKeys = { 0xAA, 0xDF, 0x29, 0x5C, 0x36, 0x90, 0x17, 0x68 };
        private static string ProjectInsideAddKey = "projectINSIDEkey";
        private static byte[] Keys = { 0xA5, 0x95, 0x6E, 0x44, 0x80, 0xCB, 0x8F, 0x77 };
        private static string AddKey = "";

        public FormInstallLicenseFile()
        {
            InitializeComponent();
        }

        string DecryptDES_ProjectInsideKey(string decryptString, string decryptKey)
        {

            try
            {
                if (decryptKey.Length < 8)
                {
                    decryptKey += ProjectInsideAddKey;
                }
                byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey.Substring(0, 8));

                byte[] rgbIV = ProjectInsideKeys;

                byte[] inputByteArray = Convert.FromBase64String(decryptString);

                DESCryptoServiceProvider DCSP = new DESCryptoServiceProvider();

                MemoryStream mStream = new MemoryStream();

                CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, rgbIV), CryptoStreamMode.Write);

                cStream.Write(inputByteArray, 0, inputByteArray.Length);

                cStream.FlushFinalBlock();

                return Encoding.UTF8.GetString(mStream.ToArray());

            }
            catch
            {
                return decryptString;
            }
        }

        string DecryptDES(string decryptString, string decryptKey)
        {

            try
            {
                if (decryptKey.Length < 8)
                {
                    decryptKey += AddKey;
                }
                byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey.Substring(0, 8));

                byte[] rgbIV = Keys;

                byte[] inputByteArray = Convert.FromBase64String(decryptString);

                DESCryptoServiceProvider DCSP = new DESCryptoServiceProvider();

                MemoryStream mStream = new MemoryStream();

                CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, rgbIV), CryptoStreamMode.Write);

                cStream.Write(inputByteArray, 0, inputByteArray.Length);

                cStream.FlushFinalBlock();

                return Encoding.UTF8.GetString(mStream.ToArray());

            }
            catch
            {
                return decryptString;
            }
        }

        private void buttonInstallLicFile_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = openFileDialog1.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                XmlDocument xdoc = new XmlDocument();
                try
                {
                    xdoc.Load(openFileDialog1.FileName);
                }
                catch (Exception ex)
                {
                    listBoxLog.Items.Add(String.Format("无效的许可文件。\r\n{0}", ex.Message));
                    return;
                }

                //verification
                try
                {
                    string endpoint = xdoc.DocumentElement["endpoint"].InnerText;
                    AddKey = DecryptDES_ProjectInsideKey(endpoint, "35405717");

                    SHA384Managed shaM = new SHA384Managed();
                    byte[] data;

                    MemoryStream ms = new MemoryStream();
                    BinaryWriter bw = new BinaryWriter(ms);
                    bw.Write(12);
                    bw.Write(xdoc.DocumentElement["level"].InnerText);
                    bw.Write(xdoc.DocumentElement["type"].InnerText);
                    bw.Write(xdoc.DocumentElement["endpoint"].InnerText);
                    bw.Write(xdoc.DocumentElement["createtime"].InnerText);
                    bw.Write(xdoc.DocumentElement["endtime"].InnerText);
                    bw.Write(xdoc.DocumentElement["guid"].InnerText);
                    XmlElement elem = (XmlElement)xdoc.DocumentElement["features"].FirstChild;
                    int nFeatures = xdoc.DocumentElement["features"].GetElementsByTagName("feature").Count;
                    for (int i = 0; i < nFeatures; i++)
                    {
                        bw.Write(elem.Attributes["name"].Value); bw.Write(elem.Attributes["value"].Value);
                        if (elem.Attributes["name"].Value == "versiontype")
                            DecryptDES(elem.Attributes["value"].Value, "35405717");
                        if (elem.Attributes["name"].Value == "siptrunk")
                            DecryptDES(elem.Attributes["value"].Value, "35405717");
                        elem = (XmlElement)elem.NextSibling;
                    }
                    int nLen = (int)ms.Position + 1;
                    bw.Close();
                    ms.Close();
                    data = ms.GetBuffer();

                    data = shaM.ComputeHash(data, 0, nLen);

                    string result = "";
                    foreach (byte dbyte in data)
                    {
                        result += dbyte.ToString("X2");
                    }
                    string signature = xdoc.DocumentElement["signature"].InnerText;
                    if (signature != result)
                    {
                        listBoxLog.Items.Add("无效的许可文件。签名错。");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    listBoxLog.Items.Add(String.Format("无效的许可文件。\r\n{0}", ex.Message));
                    return;
                }
            }
        }
    }
}
