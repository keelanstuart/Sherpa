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

		public static ServerOptionsForm optionsForm = null;

		private static AutoResetEvent shutdownEvent = new AutoResetEvent(false);     // signaled when it's time to shut down
		private static AutoResetEvent settingsEvent = new AutoResetEvent(true);		 // signaled to update the listener settings
		private static AutoResetEvent startEvent = new AutoResetEvent(false);		 // signaled to start the listener
		private static AutoResetEvent stopEvent = new AutoResetEvent(false);		 // signaled to stop the listener
		private static AutoResetEvent servedEvent = new AutoResetEvent(false);		 // signaled after each request has been served, reset when a request is received
		private static Mutex rootDirMutex = new Mutex(false);

		private static int requestCount = 0;

		public static void ListenerRequestCallback(IAsyncResult result)
		{
			HttpListener listener = (HttpListener)result.AsyncState;

			// Use EndGetContext to complete the asynchronous operation.
			HttpListenerContext ctx;
			try
			{
				ctx = listener.EndGetContext(result);
			}
			catch (Exception)               // it's possible that we closed the program...
			{
				return;
			}

			// Peel out the requests and response objects
			HttpListenerRequest req = ctx.Request;
			HttpListenerResponse resp = ctx.Response;

			// if the rootDir is blank, then we'll use the user's MyDocuments folder...
			rootDirMutex.WaitOne(-1);
			string filename = Properties.Settings.Default.rootDir;
			rootDirMutex.ReleaseMutex();

			if (filename.Length == 0)
			{
				filename = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			}

			//string reqUrl = req.Url.
			// Append the requested file to the rootDir to get the actual local filename
			filename += req.Url.AbsolutePath;

			// if the filename is actually a bare directory and an index.html file
			// exists inside it, then that's our file
			if (Directory.Exists(filename))
			{
				if (File.Exists(filename + "/index.html"))
				{
					filename += "/index.html";
				}
			}

			// Print out some info about the request
			optionsForm.Log(string.Format("Request({0}): {2} << \"{1}\" ({3})\n", ++requestCount, req.Url.ToString(), req.UserHostName, filename));

			byte[] databuf = null;

			// if the file requested doesn't exist...
			if (!File.Exists(filename))
			{
				// The 404 message
				string fourOhFour =	"<html>" +
					"\t<head>" +
					"\t\t<title>Requested File Unavailable</title>" +
					"\t</head>" +
					"\t<body>" +
					"\t\t<b>404: {0} not found!</b>" +
					"\t</body>" +
					"</html>";

				// report 404
				resp.StatusCode = 404;
				databuf = Encoding.UTF8.GetBytes(String.Format(fourOhFour, filename));
			}
			else
			{
				// otherwise, load it's contents into data and report "OK"
				resp.StatusCode = 200;
				databuf = File.ReadAllBytes(filename);
			}

			// Retrieve the content type from the registry -- they're under CLASSES_ROOT/<ext>/Content Type
			// If the extension doesn't have one, then we use application/octet-stream for binary
			string ext = Path.GetExtension(filename);
			resp.ContentType = "application/octet-stream";
			RegistryKey rk = Registry.ClassesRoot.OpenSubKey(ext);
			if (rk != null)
			{
				object tmp_type = rk.GetValue("Content Type", "application/octet-stream");
				if (tmp_type != null)
					resp.ContentType = tmp_type.ToString();
			}

			// Write the response info
			resp.ContentEncoding = Encoding.UTF8;

			// Send chunked if the data isn't "text" or it's larger than 256KB
			resp.SendChunked = true;// false;// (data.LongLength >= (1 << 18)) || !resp.ContentType.Contains("text", StringComparison.CurrentCultureIgnoreCase);
			resp.ContentLength64 = databuf.Length;

			// Write out to the response stream (asynchronously), then close it
			resp.OutputStream.WriteAsync(databuf, 0, databuf.Length);
			resp.Close();

			// Once the request has been served, we can quit if we need to
			servedEvent.Set();
		}

		public static void ServerThreadProc()
		{
			WaitHandle[] waitEvents = new WaitHandle[5] { shutdownEvent, settingsEvent, startEvent, stopEvent, servedEvent  };

			// Create a Http server and start listening for incoming connections
			HttpListener listener = new HttpListener();

			IAsyncResult ctx = null;
			string url = "";

			// Handle requests while the shutdown event hasn't been signaled
			int waitret = WaitHandle.WaitTimeout;
			while ((waitret = WaitHandle.WaitAny(waitEvents, -1)) != 0)
			{
				switch (waitret)
				{
					case 1:
					{
						// if we're already listening, then we'll need to stop and start again with a new port #
						bool restart = listener.IsListening;
						if (restart)
						{
							optionsForm.SetServerState(ServerOptionsForm.ServerState.Paused);
							listener.Stop();
							ctx.AsyncWaitHandle.WaitOne(-1);
							servedEvent.Reset();
						}

						// build the listener url string using the designated port.
						url = "http://localhost:";
						url += Properties.Settings.Default.port.ToString();
						url += "/";

						listener.Prefixes.Clear();
						listener.Prefixes.Add(url);

						if (restart)
							startEvent.Set();
						break;
					}

					case 2:     // start
						if (!listener.IsListening)
						{
							// start the listener...
							listener.Start();
							optionsForm.Log(string.Format("Listening for connections as {0}\n", url));

							try
							{
								// but sometimes things get whacked when re-starting the async context
								ctx = listener.BeginGetContext(new AsyncCallback(ListenerRequestCallback), listener);
							}
							catch (Exception)
							{
								// if that happens, re-initialize the listener
								listener.Close();
								listener = new HttpListener();
								listener.Prefixes.Clear();
								listener.Prefixes.Add(url);
								startEvent.Set();
							}
						}

						optionsForm.SetServerState(ServerOptionsForm.ServerState.Active);
						break;

					case 3:     // stop
						if (listener.IsListening)
						{
							listener.Stop();
							ctx.AsyncWaitHandle.WaitOne(-1);
						}

						optionsForm.SetServerState(ServerOptionsForm.ServerState.Paused);
						break;

					case 4:     // serve completed, so refresh the context
						if (listener.IsListening)
							ctx = listener.BeginGetContext(new AsyncCallback(ListenerRequestCallback), listener);
						break;
				}
			}

			// Close the listener
			listener.Close();
		}

		public static void StartServer()
		{
			startEvent.Set();
		}

		public static void StopServer()
		{
			stopEvent.Set();
		}

		public static void UpdatePort(int value)
		{
			Properties.Settings.Default.port = value;
			settingsEvent.Set();
		}

		public static void UpdateRootDir(string value)
		{
			if (value != Properties.Settings.Default.rootDir)
			{
				rootDirMutex.WaitOne(-1);
				Properties.Settings.Default.rootDir = value;
				rootDirMutex.ReleaseMutex();
			}

			optionsForm.Log(string.Format("Serving content from file://{0}\n", value));
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
			catch (Exception)	// ignore any exceptions -- things "not working" means things are "working"
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

			optionsForm = new ServerOptionsForm();

			UpdateRootDir(Properties.Settings.Default.rootDir);

			// Start the server thread
			Thread serverThread = new Thread(new ThreadStart(ServerThreadProc));
			serverThread.Start();

			Application.Run(optionsForm);

			// tell the server thread that it needs to shut down...
			shutdownEvent.Set();

			// ...and wait for the thread to stop.
			serverThread.Join();

			// We're closing now, so close the "single instance" system global mutex
			m.Close();
		}
	}
}
