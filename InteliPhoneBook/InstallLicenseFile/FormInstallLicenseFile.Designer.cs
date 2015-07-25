namespace InstallLicenseFile
{
    partial class FormInstallLicenseFile
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonInstallLicFile = new System.Windows.Forms.Button();
            this.listBoxLog = new System.Windows.Forms.ListBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // buttonInstallLicFile
            // 
            this.buttonInstallLicFile.Location = new System.Drawing.Point(204, 7);
            this.buttonInstallLicFile.Name = "buttonInstallLicFile";
            this.buttonInstallLicFile.Size = new System.Drawing.Size(145, 20);
            this.buttonInstallLicFile.TabIndex = 0;
            this.buttonInstallLicFile.Text = "安装";
            this.buttonInstallLicFile.UseVisualStyleBackColor = true;
            this.buttonInstallLicFile.Click += new System.EventHandler(this.buttonInstallLicFile_Click);
            // 
            // listBoxLog
            // 
            this.listBoxLog.FormattingEnabled = true;
            this.listBoxLog.HorizontalScrollbar = true;
            this.listBoxLog.ItemHeight = 12;
            this.listBoxLog.Location = new System.Drawing.Point(12, 34);
            this.listBoxLog.Name = "listBoxLog";
            this.listBoxLog.ScrollAlwaysVisible = true;
            this.listBoxLog.Size = new System.Drawing.Size(530, 220);
            this.listBoxLog.TabIndex = 1;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "lic";
            this.openFileDialog1.Filter = "许可文件|*.xml";
            // 
            // FormInstallLicenseFile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(554, 273);
            this.Controls.Add(this.listBoxLog);
            this.Controls.Add(this.buttonInstallLicFile);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "FormInstallLicenseFile";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "安装许可文件";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonInstallLicFile;
        private System.Windows.Forms.ListBox listBoxLog;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
    }
}

