﻿namespace IPBDialWSTortureTest
{
    partial class IPBDialWSTortureTestForm
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
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.checkBoxFSUp = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 237);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(89, 24);
            this.button1.TabIndex = 0;
            this.button1.Text = "Create Thread";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(387, 236);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(120, 24);
            this.button2.TabIndex = 1;
            this.button2.Text = "Terminate Thread";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // checkBoxFSUp
            // 
            this.checkBoxFSUp.AutoSize = true;
            this.checkBoxFSUp.Location = new System.Drawing.Point(12, 12);
            this.checkBoxFSUp.Name = "checkBoxFSUp";
            this.checkBoxFSUp.Size = new System.Drawing.Size(102, 16);
            this.checkBoxFSUp.TabIndex = 2;
            this.checkBoxFSUp.Text = "FreeSWITCH Up";
            this.checkBoxFSUp.UseVisualStyleBackColor = true;
            // 
            // IPBDialWSTortureTestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(547, 273);
            this.Controls.Add(this.checkBoxFSUp);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Name = "IPBDialWSTortureTestForm";
            this.Text = "Inteli Phone Book Dial WebService Torture Test";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.CheckBox checkBoxFSUp;
    }
}
