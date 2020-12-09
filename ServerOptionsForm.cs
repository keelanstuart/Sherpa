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

			Properties.Settings.Default.previousRootDirs = new System.Collections.Specialized.StringCollection();
			if (Properties.Settings.Default.previousRootDirs != null)
			{
				var it = Properties.Settings.Default.previousRootDirs.GetEnumerator();
				while (it.MoveNext())
				{
					comboRoot.Items.Add(it.ToString());
				}
			}

			Sherpa.StartServer();
		}

		private void ServerOptionsForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			Sherpa.StopServer();

			Properties.Settings.Default.previousRootDirs.Clear();
			var it = comboRoot.Items.GetEnumerator();
			while (it.MoveNext())
			{
				Properties.Settings.Default.previousRootDirs.Add(it.Current.ToString());
			}

			Properties.Settings.Default.Save();
		}

		private void editPort_ValueChanged(object sender, EventArgs e)
		{
			Sherpa.UpdatePort((int)editPort.Value);
		}

		private void comboRoot_TextUpdate(object sender, EventArgs e)
		{
			if (Directory.Exists(comboRoot.Text))
			{
				// Search for the path in the combobox items...
				bool itemfound = false;

				// use the lower case version of comboRoot.Text
				string temp = comboRoot.Text;
				while (temp.EndsWith('\\') || temp.EndsWith('/'))
					temp = temp.Remove(temp.Length - 1);
				string templc = temp.ToLower();

				var it = comboRoot.Items.GetEnumerator();
				while (it.MoveNext())
				{
					// only compare like cases
					if (templc == it.Current.ToString().ToLower())
					{	
						itemfound = true;
						break;
					}
				}

				// if it hasn't been entered before, then add it to the combobox's drop down list...
				// this will be used later to populate a sub-menu on the tray icon for fast switching
				if (!itemfound)
					comboRoot.Items.Add(temp);

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
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (Sherpa.ServerActive())
			{
				Sherpa.StopServer();
			}
			else
			{
				Sherpa.StartServer();
			}
		}

		private void comboRoot_SelectionChangeCommitted(object sender, EventArgs e)
		{
			Sherpa.UpdateRootDir(comboRoot.SelectedItem.ToString());
		}
	}
}
