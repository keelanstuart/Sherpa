using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Text;
using System.Net;
using Microsoft.Win32;
using System.Web;

namespace Sherpa
{
	static class Sherpa
	{
		// used as a key to a global mutex that keeps us from starting multiple instances
		private static string instanceMutexName = "SherpaInstance";

		private static ServerOptionsForm optionsForm = null;

		private static HttpServer server = null;

		public static void LogFunc(string s)
		{
			if (optionsForm != null)
			{
				optionsForm.Log(s);
			}
		}

		public static void ServerStateFunc(HttpServer.State value)
		{
			if (optionsForm != null)
			{
				optionsForm.SetServerState(value);
			}
		}

		public static void StartServer()
		{
			server.StartServer();
		}

		public static void StopServer()
		{
			server.StopServer();
		}

		public static void UpdatePort(int value)
		{
			if (Properties.Settings.Default.port != value)
			{
				Properties.Settings.Default.port = value;
				server.UpdatePort(Properties.Settings.Default.port);
			}
		}

		public static void UpdateRootDir(string value)
		{
			if (Properties.Settings.Default.rootDir != value)
			{
				Properties.Settings.Default.rootDir = value;
				server.UpdateRootDir(Properties.Settings.Default.rootDir);
			}
		}

		[STAThread]
		public static void Main()
		{
			// This code ensures that only one instance of the program is running at a time...
			Mutex m = null;
			try
			{
				m = Mutex.OpenExisting(instanceMutexName);
			}
			catch (Exception)   // ignore any exceptions -- things "not working" means things are "working"
			{
			}

			// if we could get the mutex, then another instance is already running, so quit...
			if (m != null)
			{
				MessageBox.Show("Only one instance of Sherpa is allowed at a time.");
				return;
			}

			// Create our system global mutex that signifies our process is running
			m = new Mutex(true, instanceMutexName);

			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			server = new HttpServer();
			optionsForm = new ServerOptionsForm();

			server.SetLogFunc(LogFunc);
			server.SetReportStateFunc(ServerStateFunc);

			server.UpdateRootDir(Properties.Settings.Default.rootDir);
			server.UpdatePort(Properties.Settings.Default.port);

			Application.Run(optionsForm);

			server.StopServer();
			server.Dispose();

			// We're closing now, so close the "single instance" system global mutex
			m.Close();
		}
	}
}
