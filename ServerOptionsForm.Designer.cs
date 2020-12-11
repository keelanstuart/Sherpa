using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Net;
using Microsoft.Win32;
using System.Web;

namespace Sherpa
{
	partial class ServerOptionsForm
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServerOptionsForm));
			this.logTextBox = new System.Windows.Forms.RichTextBox();
			this.editPort = new System.Windows.Forms.NumericUpDown();
			this.labelPort = new System.Windows.Forms.Label();
			this.comboRoot = new System.Windows.Forms.ComboBox();
			this.labelRoot = new System.Windows.Forms.Label();
			this.buttonBrowseRoot = new System.Windows.Forms.Button();
			this.rootBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.buttonActive = new System.Windows.Forms.Button();
			this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
			this.notifyIconContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.menuItemOpen = new System.Windows.Forms.ToolStripMenuItem();
			this.menuItemToggleActivate = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.menuItemRootDirQuickChange = new System.Windows.Forms.ToolStripMenuItem();
			((System.ComponentModel.ISupportInitialize)(this.editPort)).BeginInit();
			this.notifyIconContextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// logTextBox
			// 
			this.logTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.logTextBox.BackColor = System.Drawing.SystemColors.AppWorkspace;
			this.logTextBox.Location = new System.Drawing.Point(6, 120);
			this.logTextBox.Name = "logTextBox";
			this.logTextBox.ReadOnly = true;
			this.logTextBox.Size = new System.Drawing.Size(562, 247);
			this.logTextBox.TabIndex = 0;
			this.logTextBox.Text = "";
			this.logTextBox.WordWrap = false;
			// 
			// editPort
			// 
			this.editPort.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.editPort.Location = new System.Drawing.Point(95, 51);
			this.editPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
			this.editPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.editPort.Name = "editPort";
			this.editPort.Size = new System.Drawing.Size(439, 23);
			this.editPort.TabIndex = 2;
			this.editPort.Value = new decimal(new int[] {
            8000,
            0,
            0,
            0});
			this.editPort.ValueChanged += new System.EventHandler(this.editPort_ValueChanged);
			// 
			// labelPort
			// 
			this.labelPort.AutoSize = true;
			this.labelPort.Location = new System.Drawing.Point(5, 54);
			this.labelPort.Name = "labelPort";
			this.labelPort.Size = new System.Drawing.Size(29, 15);
			this.labelPort.TabIndex = 3;
			this.labelPort.Text = "Port";
			// 
			// comboRoot
			// 
			this.comboRoot.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.comboRoot.FormattingEnabled = true;
			this.comboRoot.Location = new System.Drawing.Point(95, 80);
			this.comboRoot.Name = "comboRoot";
			this.comboRoot.Size = new System.Drawing.Size(439, 23);
			this.comboRoot.TabIndex = 4;
			this.comboRoot.SelectionChangeCommitted += new System.EventHandler(this.comboRoot_SelectionChangeCommitted);
			this.comboRoot.TextUpdate += new System.EventHandler(this.comboRoot_TextUpdate);
			// 
			// labelRoot
			// 
			this.labelRoot.AutoSize = true;
			this.labelRoot.Location = new System.Drawing.Point(6, 84);
			this.labelRoot.Name = "labelRoot";
			this.labelRoot.Size = new System.Drawing.Size(83, 15);
			this.labelRoot.TabIndex = 5;
			this.labelRoot.Text = "Root Directory";
			// 
			// buttonBrowseRoot
			// 
			this.buttonBrowseRoot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonBrowseRoot.Location = new System.Drawing.Point(540, 80);
			this.buttonBrowseRoot.Name = "buttonBrowseRoot";
			this.buttonBrowseRoot.Size = new System.Drawing.Size(28, 23);
			this.buttonBrowseRoot.TabIndex = 6;
			this.buttonBrowseRoot.Text = "...";
			this.buttonBrowseRoot.UseVisualStyleBackColor = true;
			this.buttonBrowseRoot.Click += new System.EventHandler(this.buttonBrowseRoot_Click);
			// 
			// rootBrowserDialog
			// 
			this.rootBrowserDialog.Description = "Select the Root Directory for Serving Files";
			this.rootBrowserDialog.RootFolder = System.Environment.SpecialFolder.UserProfile;
			this.rootBrowserDialog.UseDescriptionForTitle = true;
			// 
			// buttonActive
			// 
			this.buttonActive.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonActive.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.buttonActive.BackColor = System.Drawing.Color.Moccasin;
			this.buttonActive.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.buttonActive.ForeColor = System.Drawing.Color.Black;
			this.buttonActive.Location = new System.Drawing.Point(6, 7);
			this.buttonActive.Name = "buttonActive";
			this.buttonActive.Size = new System.Drawing.Size(562, 26);
			this.buttonActive.TabIndex = 7;
			this.buttonActive.Text = "...";
			this.buttonActive.UseVisualStyleBackColor = false;
			// 
			// notifyIcon
			// 
			this.notifyIcon.BalloonTipTitle = "Sherpa";
			this.notifyIcon.ContextMenuStrip = this.notifyIconContextMenu;
			this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
			this.notifyIcon.Visible = true;
			this.notifyIcon.DoubleClick += new System.EventHandler(this.notifyIcon_DoubleClick);
			// 
			// notifyIconContextMenu
			// 
			this.notifyIconContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuItemOpen,
            this.menuItemToggleActivate,
            this.toolStripSeparator1,
            this.menuItemRootDirQuickChange});
			this.notifyIconContextMenu.Name = "notifyIconContextMenu";
			this.notifyIconContextMenu.Size = new System.Drawing.Size(200, 76);
			// 
			// menuItemOpen
			// 
			this.menuItemOpen.Name = "menuItemOpen";
			this.menuItemOpen.Size = new System.Drawing.Size(199, 22);
			this.menuItemOpen.Text = "Open Options Dialog...";
			// 
			// menuItemToggleActivate
			// 
			this.menuItemToggleActivate.Name = "menuItemToggleActivate";
			this.menuItemToggleActivate.Size = new System.Drawing.Size(199, 22);
			this.menuItemToggleActivate.Text = "Disable";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(196, 6);
			// 
			// menuItemRootDirQuickChange
			// 
			this.menuItemRootDirQuickChange.Enabled = false;
			this.menuItemRootDirQuickChange.Name = "menuItemRootDirQuickChange";
			this.menuItemRootDirQuickChange.Size = new System.Drawing.Size(199, 22);
			this.menuItemRootDirQuickChange.Text = "Quickset Root Directory";
			// 
			// ServerOptionsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(573, 372);
			this.Controls.Add(this.buttonActive);
			this.Controls.Add(this.buttonBrowseRoot);
			this.Controls.Add(this.labelRoot);
			this.Controls.Add(this.comboRoot);
			this.Controls.Add(this.labelPort);
			this.Controls.Add(this.editPort);
			this.Controls.Add(this.logTextBox);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ServerOptionsForm";
			this.Text = "Sherpa HTTP Server";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ServerOptionsForm_FormClosed);
			this.Load += new System.EventHandler(this.ServerOptionsForm_Load);
			((System.ComponentModel.ISupportInitialize)(this.editPort)).EndInit();
			this.notifyIconContextMenu.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.RichTextBox logTextBox;
		private System.Windows.Forms.NumericUpDown editPort;
		private System.Windows.Forms.Label labelPort;
		private System.Windows.Forms.ComboBox comboRoot;
		private System.Windows.Forms.Label labelRoot;
		private System.Windows.Forms.Button buttonBrowseRoot;
		private FolderBrowserDialog rootBrowserDialog;
		public System.Windows.Forms.Button buttonActive;
		private NotifyIcon notifyIcon;
		private ContextMenuStrip notifyIconContextMenu;
		private ToolStripMenuItem menuItemOpen;
		private ToolStripMenuItem menuItemToggleActivate;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripMenuItem menuItemRootDirQuickChange;
	}
}

