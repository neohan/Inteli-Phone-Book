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
using System.Management;
using System.Net.NetworkInformation;

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

        private static string GetCpuId()
        {
            string strCpuid = "";
            try
            {
                ManagementClass mcCpu = new ManagementClass("win32_Processor");
                ManagementObjectCollection mocCpu = mcCpu.GetInstances();

                foreach (ManagementObject m in mocCpu)
                {
                    strCpuid = m["ProcessorId"].ToString();
                    if (strCpuid != null)
                    {
                        break;
                    }
                }

                return strCpuid;
            }
            catch
            {
                return strCpuid;
            }
        }

        private static string GetDiskId()
        {
            string diskId = "";

            try
            {
                ManagementObjectSearcher wmiSearcher = new ManagementObjectSearcher();

                wmiSearcher.Query = new SelectQuery("Win32_DiskDrive",
                                                    "",
                                                    new string[] { "PNPDeviceID" });
                ManagementObjectCollection myCollection = wmiSearcher.Get();
                ManagementObjectCollection.ManagementObjectEnumerator em =
                myCollection.GetEnumerator();
                em.MoveNext();
                ManagementBaseObject mo = em.Current;
                diskId = mo.Properties["PNPDeviceID"].Value.ToString().Trim();
                return diskId;
            }
            catch
            {
                return diskId;
            }
        }

        private static string GetMacId()
        {
            string macId = "";

            try
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    NetworkInterface[] ifaces = NetworkInterface.GetAllNetworkInterfaces();
                    PhysicalAddress address = ifaces[0].GetPhysicalAddress();
                    byte[] byteAddr = address.GetAddressBytes();
                    for (int i = 0; i < byteAddr.Length; i++)
                    {
                        macId += byteAddr[i].ToString("X2");
                        if (i != byteAddr.Length - 1)
                        {
                            macId += "-";
                        }
                    }
                }
                return macId;
            }
            catch
            {
                return macId;
            }
        }

        private string EncryptDES_ProjectInsideKey(string encryptString, string encryptKey)
        {
            try
            {
                if (encryptKey.Length < 8)
                {
                    encryptKey += ProjectInsideAddKey;
                }

                byte[] rgbKey = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));

                byte[] rgbIV = ProjectInsideKeys;

                byte[] inputByteArray = Encoding.UTF8.GetBytes(encryptString);

                DESCryptoServiceProvider dCSP = new DESCryptoServiceProvider();

                MemoryStream mStream = new MemoryStream();

                CryptoStream cStream = new CryptoStream(mStream, dCSP.CreateEncryptor(rgbKey, rgbIV), CryptoStreamMode.Write);

                cStream.Write(inputByteArray, 0, inputByteArray.Length);

                cStream.FlushFinalBlock();

                return Convert.ToBase64String(mStream.ToArray());

            }
            catch
            {
                return encryptString;
            }
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

        bool CheckLicenseFile(string p_licfile)
        {
            string thisEndpointKey = EncryptDES_ProjectInsideKey(String.Format("{0}-{1}-{2}", GetCpuId(), GetDiskId(), GetMacId()), "35405717");
            XmlDocument xdoc = new XmlDocument();
            try
            {
                xdoc.Load(p_licfile);
            }
            catch (Exception ex)
            {
                listBoxLog.Items.Add(String.Format("无效的许可文件。\r\n{0}", ex.Message));
                return false;
            }

            //verification
            try
            {
                string endpoint = xdoc.DocumentElement["endpoint"].InnerText;
                if (endpoint != thisEndpointKey)
                {
                    listBoxLog.Items.Add("无效的许可文件。机器信息不符。");
                    return false;
                }
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
                string versionType = "", sipTrunks = "";
                for (int i = 0; i < nFeatures; i++)
                {
                    bw.Write(elem.Attributes["name"].Value); bw.Write(elem.Attributes["value"].Value);
                    if (elem.Attributes["name"].Value == "versiontype")
                        versionType = DecryptDES(elem.Attributes["value"].Value, "35405717");
                    if (elem.Attributes["name"].Value == "siptrunk")
                        sipTrunks = DecryptDES(elem.Attributes["value"].Value, "35405717");
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
                    return false;
                }
                if ( versionType == "basic" )
                    listBoxLog.Items.Add("基本版");
                else if ( versionType == "enforce" )
                    listBoxLog.Items.Add("加强版。");
                else if (versionType == "custom")
                    listBoxLog.Items.Add("定制版。");
                else
                {
                    listBoxLog.Items.Add("无效的许可文件。版本类型错。");
                    return false;
                }
                listBoxLog.Items.Add(String.Format("中继数:{0}", sipTrunks));
            }
            catch (Exception ex)
            {
                listBoxLog.Items.Add(String.Format("无效的许可文件。\r\n{0}", ex.Message));
                return false;
            }
            return true;
        }

        private void buttonInstallLicFile_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = openFileDialog1.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                listBoxLog.Items.Add("检查待安装的许可文件...");
                bool bCopyingLicFileIsValid = CheckLicenseFile(openFileDialog1.FileName);
                if (bCopyingLicFileIsValid == false)
                {//提示许可文件无效
                    MessageBox.Show("待安装的许可文件无效！", "错误", MessageBoxButtons.OK);
                    return;
                }

                bool bUsingLicFileIsValid = false;
                int pos = Application.ExecutablePath.LastIndexOf("\\");
                string path = Application.ExecutablePath.Substring(0, pos);
                if (File.Exists(path + "\\lic.xml"))
                {
                    listBoxLog.Items.Add("检查已存在的许可文件...");
                    bUsingLicFileIsValid = CheckLicenseFile(path + "\\lic.xml");
                }

                bool bDoCopy;
                if (bUsingLicFileIsValid == false)
                {
                    bDoCopy = true;
                }
                else
                {//提示是否确认要拷贝，因为正在使用的许可文件也是有效的。
                    DialogResult yesnoDialogResult = MessageBox.Show("确认替换已有的许可！", "提示", MessageBoxButtons.YesNo);
                    if( yesnoDialogResult == DialogResult.Yes )
                        bDoCopy = true;
                    else
                        bDoCopy = false;
                }

                if (bDoCopy)
                {//拷贝许可文件
                    try
                    {
                        File.Copy(openFileDialog1.FileName, path + "\\lic.xml", true);
                        listBoxLog.Items.Add("安装许可文件成功！");
                        MessageBox.Show("安装许可文件成功！", "提示", MessageBoxButtons.OK);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        listBoxLog.Items.Add("无法安装许可文件，请以管理员权限运行此程序。");
                    }
                    catch (Exception otherex)
                    {
                        listBoxLog.Items.Add("无法安装许可文件，请稍后再试，如反复出现请联系管理员！");
                        listBoxLog.Items.Add(otherex.Message);
                    }
                }
            }
        }
    }
}
