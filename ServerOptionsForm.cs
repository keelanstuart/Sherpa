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
				comboRoot.Items.Add(Properties.Settings.Default.previousRootDirs[i].ToString());
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
				comboRoot.Items.Add(path);
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
			Show();
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

		public enum ServerState { Active, Paused, Waiting };

		public void SetServerState(ServerState s)
		{
			if (buttonActive == null)
				return;

			if (InvokeRequired)
			{
				try
				{
					this.Invoke(new Action<ServerState>(SetServerState), new object[] { s });
				}
				catch(Exception)
				{
				}

				return;
			}

			switch (s)
			{
				case ServerState.Active:
					buttonActive.Text = "Active";
					buttonActive.BackColor = Color.PaleGreen;
					buttonActive.ForeColor = Color.Black;
					buttonActive.Click -= buttonPaused_Click;
					buttonActive.Click += buttonActive_Click;
					buttonActive.Enabled = true;
					break;

				case ServerState.Paused:
					buttonActive.Text = "Paused";
					buttonActive.BackColor = Color.LightCoral;
					buttonActive.ForeColor = Color.White;
					buttonActive.Click -= buttonActive_Click;
					buttonActive.Click += buttonPaused_Click;
					buttonActive.Enabled = true;
					break;

				case ServerState.Waiting:
					buttonActive.Text = "...";
					buttonActive.BackColor = Color.Moccasin;
					buttonActive.ForeColor = Color.White;
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
	}
}
