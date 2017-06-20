﻿namespace TweetDuck.Core.Other {
    partial class FormPlugins {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.btnClose = new System.Windows.Forms.Button();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnOpenFolder = new System.Windows.Forms.Button();
            this.flowLayoutPlugins = new TweetDuck.Plugins.Controls.PluginListFlowLayout();
            this.SuspendLayout();
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.AutoSize = true;
            this.btnClose.Location = new System.Drawing.Point(643, 439);
            this.btnClose.Name = "btnClose";
            this.btnClose.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.btnClose.Size = new System.Drawing.Size(49, 23);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnReload
            // 
            this.btnReload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnReload.AutoSize = true;
            this.btnReload.Location = new System.Drawing.Point(131, 439);
            this.btnReload.Name = "btnReload";
            this.btnReload.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.btnReload.Size = new System.Drawing.Size(71, 23);
            this.btnReload.TabIndex = 2;
            this.btnReload.Text = "Reload All";
            this.btnReload.UseVisualStyleBackColor = true;
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOpenFolder.AutoSize = true;
            this.btnOpenFolder.Location = new System.Drawing.Point(12, 439);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.btnOpenFolder.Size = new System.Drawing.Size(113, 23);
            this.btnOpenFolder.TabIndex = 3;
            this.btnOpenFolder.Text = "Open Plugin Folder";
            this.btnOpenFolder.UseVisualStyleBackColor = true;
            this.btnOpenFolder.Click += new System.EventHandler(this.btnOpenFolder_Click);
            // 
            // flowLayoutPlugins
            // 
            this.flowLayoutPlugins.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPlugins.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flowLayoutPlugins.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPlugins.Location = new System.Drawing.Point(12, 12);
            this.flowLayoutPlugins.Name = "flowLayoutPlugins";
            this.flowLayoutPlugins.Size = new System.Drawing.Size(680, 421);
            this.flowLayoutPlugins.TabIndex = 0;
            this.flowLayoutPlugins.WrapContents = false;
            this.flowLayoutPlugins.Resize += new System.EventHandler(this.flowLayoutPlugins_Resize);
            // 
            // FormPlugins
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(704, 474);
            this.Controls.Add(this.flowLayoutPlugins);
            this.Controls.Add(this.btnOpenFolder);
            this.Controls.Add(this.btnReload);
            this.Controls.Add(this.btnClose);
            this.Icon = global::TweetDuck.Properties.Resources.icon;
            this.MinimumSize = new System.Drawing.Size(480, 320);
            this.Name = "FormPlugins";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnOpenFolder;
        private Plugins.Controls.PluginListFlowLayout flowLayoutPlugins;
    }
}