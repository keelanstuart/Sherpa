using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Sherpa.Properties;

namespace Sherpa
{
	public partial class ServerOptionsForm : Form
	{
		public ServerOptionsForm()
		{
			InitializeComponent();
		}

		private void ServerOptionsForm_Load(object sender, EventArgs e)
		{
			comboRoot.Text = Properties.Settings.Default.rootDir;
			editPort.Value = Properties.Settings.Default.port;

			for (int i = 0; i < Properties.Settings.Default.previousRootDirs.Count; i++)
			{
				MaybeAddPathToDropDown(Properties.Settings.Default.previousRootDirs[i].ToString());
			}

			Sherpa.StartServer();
		}

		private void ServerOptionsForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			Sherpa.StopServer();

			var it = comboRoot.Items.GetEnumerator();
			while (it.MoveNext())
			{
				if (!Properties.Settings.Default.previousRootDirs.Contains(it.Current.ToString()))
					Properties.Settings.Default.previousRootDirs.Add(it.Current.ToString());
			}

			Properties.Settings.Default.Save();
		}

		private void editPort_ValueChanged(object sender, EventArgs e)
		{
			Sherpa.UpdatePort((int)editPort.Value);
		}

		private void MaybeAddPathToDropDown(string path)
		{
			// Search for the path in the combobox items...
			bool itemfound = false;

			// use the lower case version of comboRoot.Text
			string temp = comboRoot.Text;
			while (path.EndsWith('\\') || path.EndsWith('/'))
				path = path.Remove(path.Length - 1);
			string pathlc = path.ToLower();

			var it = comboRoot.Items.GetEnumerator();
			while (it.MoveNext())
			{
				// only compare like cases
				if (pathlc == it.Current.ToString().ToLower())
				{
					itemfound = true;
					break;
				}
			}

			// if it hasn't been entered before, then add it to the combobox's drop down list...
			// this will be used later to populate a sub-menu on the tray icon for fast switching
			if (!itemfound)
			{
				comboRoot.Items.Add(path);

				menuItemRootDirQuickChange.Enabled = true;
				ToolStripItem newitem = menuItemRootDirQuickChange.DropDown.Items.Add(path);
				newitem.Click += notifyIconContextMenu_PathClicked;
			}
		}

		private void notifyIconContextMenu_PathClicked(object sender, EventArgs e)
		{
			ToolStripItem item = (ToolStripItem)sender;
			comboRoot.Text = item.Text;
			Sherpa.UpdateRootDir(item.Text);
		}

		private void comboRoot_TextUpdate(object sender, EventArgs e)
		{
			if (Directory.Exists(comboRoot.Text))
			{
				MaybeAddPathToDropDown(comboRoot.Text);
				Sherpa.UpdateRootDir(comboRoot.Text);
			}
		}

		private void buttonBrowseRoot_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog fbd = new FolderBrowserDialog();
			fbd.SelectedPath = comboRoot.Text;

			if (fbd.ShowDialog() == DialogResult.OK)
			{
				comboRoot.Text = fbd.SelectedPath;
				MaybeAddPathToDropDown(comboRoot.Text);
				Sherpa.UpdateRootDir(fbd.SelectedPath);
			}
		}

		private void comboRoot_SelectionChangeCommitted(object sender, EventArgs e)
		{
			Sherpa.UpdateRootDir(comboRoot.SelectedItem.ToString());
		}

		private void notifyIcon_DoubleClick(object sender, EventArgs e)
		{
			Visible = true;
			WindowState = FormWindowState.Normal;
		}

		public void Log(string s)
		{
			if (logTextBox == null)
				return;

			if (InvokeRequired)
			{
				try
				{
					this.Invoke(new Action<string>(Log), new object[] { s });
				}
				catch (Exception)
				{
				}

				return;
			}

			logTextBox.AppendText(s);
			logTextBox.ScrollToCaret();
		}

		public void SetServerState(HttpServer.State s)
		{
			if (buttonActive == null)
				return;

			if (InvokeRequired)
			{
				try
				{
					this.Invoke(new Action<HttpServer.State>(SetServerState), new object[] { s });
				}
				catch(Exception)
				{
				}

				return;
			}

			switch (s)
			{
				case HttpServer.State.Active:
					buttonActive.Text = "Active";
					buttonActive.BackColor = Color.PaleGreen;
					buttonActive.ForeColor = Color.Black;
					buttonActive.Click -= buttonPaused_Click;
					buttonActive.Click += buttonActive_Click;
					buttonActive.Enabled = true;

					menuItemToggleActivate.CheckState = CheckState.Checked;
					break;

				case HttpServer.State.Inactive:
					buttonActive.Text = "Paused";
					buttonActive.BackColor = Color.LightCoral;
					buttonActive.ForeColor = Color.White;
					buttonActive.Click -= buttonActive_Click;
					buttonActive.Click += buttonPaused_Click;
					buttonActive.Enabled = true;

					menuItemToggleActivate.CheckState = CheckState.Unchecked;
					break;

				case HttpServer.State.Waiting:
					buttonActive.Text = "...";
					buttonActive.BackColor = Color.Moccasin;
					buttonActive.ForeColor = Color.White;

					menuItemToggleActivate.CheckState = CheckState.Indeterminate;
					break;
			}
		}

		private void buttonActive_Click(object sender, EventArgs e)
		{
			buttonActive.Enabled = false;
			Sherpa.StopServer();
		}

		private void buttonPaused_Click(object sender, EventArgs e)
		{
			buttonActive.Enabled = false;
			Sherpa.StartServer();
		}

		private void notifyIconContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			ToolStripItem item = e.ClickedItem;

			if (item == menuItemOpen)
			{
				Visible = true;
				WindowState = FormWindowState.Normal;
			}
			else if (item == menuItemToggleActivate)
			{
				if (menuItemToggleActivate.CheckState == CheckState.Unchecked)
					Sherpa.StartServer();
				else
					Sherpa.StopServer();
			}
			else if (item == menuItemShutdown)
			{
				Application.Exit();
			}
		}

		private void ServerOptionsForm_Resize(object sender, EventArgs e)
		{
		}

		private void ServerOptionsForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.UserClosing)
			{
				e.Cancel = true;
				WindowState = FormWindowState.Minimized;
				Visible = false;
			}
		}
	}
}
