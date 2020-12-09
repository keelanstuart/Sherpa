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
		static string instanceMutexName = "SherpaInstance";

		static ServerOptionsForm optionsForm = null;

		public static HttpListener listener = null;
		public static AutoResetEvent stopEvent = null;
		public static AutoResetEvent servedEvent = null;
		public static Thread serverThread = null;

		public static string rootDir = "D:/proj/three.js/examples";
		public static int pageViews = 0;
		public static int requestCount = 0;

		// The 404 message
		public static string fourOhFour =
			"<!DOCTYPE>" +
			"<html>" +
			"  <head>" +
			"    <title>Requested File Unavailable</title>" +
			"  </head>" +
			"  <body>" +
			"    <b>404: {0} not found!</b>" +
			"  </body>" +
			"</html>";

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
				servedEvent.Set();
				return;
			}

			// Peel out the requests and response objects
			HttpListenerRequest req = ctx.Request;
			HttpListenerResponse resp = ctx.Response;

			// Print out some info about the request
			optionsForm.Log(string.Format("Request({0}): \"{1}\" <<== {2}\n", ++requestCount, req.Url.ToString(), req.UserHostName));

			// if the rootDir is blank, then we'll use the user's MyDocuments folder...
			string filename;
			if (rootDir.Length == 0)
			{
				filename = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			}
			else
			{
				filename = rootDir;
			}

			//string reqUrl = req.Url.
			// Append the requested file to the rootDir to get the actual local filename
			filename += req.Url.AbsolutePath;

			byte[] data = null;

			// if the file requested doesn't exist...
			if (!File.Exists(filename))
			{
				// report 404
				resp.StatusCode = 404;
				data = Encoding.UTF8.GetBytes(String.Format(fourOhFour, filename));
			}
			else
			{
				// otherwise, load it's contents into data and report "OK"
				resp.StatusCode = 200;
				data = File.ReadAllBytes(filename);
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
			resp.ContentLength64 = data.LongLength;

			// Write out to the response stream (asynchronously), then close it
			resp.OutputStream.WriteAsync(data, 0, data.Length);
			resp.Close();

			servedEvent.Set();
		}

		public static void ServerThreadProc()
		{
			// Create a Http server and start listening for incoming connections
			listener = new HttpListener();

			// build the listener url string using the designated port.
			string url = "http://localhost:";
			url += Properties.Settings.Default.port.ToString();
			url += "/";

			listener.Prefixes.Add(url);
			listener.Start();
			optionsForm.Log(string.Format("Listening for connections as {0}\n\n", url));

			// Handle requests
			// while the stop event hasn't been signaled
			while (!stopEvent.WaitOne(0))
			{
				servedEvent.Reset();

				// Start a timed, asynchronous wait to get the context
				var ctx = listener.BeginGetContext(new AsyncCallback(ListenerRequestCallback), listener);

				// if we actually received a request, then wait until it's been served, otherwise time out and see if we've been shut down...
				if (ctx.AsyncWaitHandle.WaitOne(100, true))
				{
					servedEvent.WaitOne();
				}
			}

			// Close the listener
			listener.Close();
		}

		public static void StartServer()
		{
			// create and start a new thread if there isn't one already
			if (serverThread == null)
			{
				serverThread = new Thread(new ThreadStart(ServerThreadProc));
				serverThread.Start();

				optionsForm.buttonActive.Text = "Active";
				optionsForm.buttonActive.BackColor = Color.PaleGreen;
				optionsForm.buttonActive.ForeColor = Color.Black;
			}
		}

		public static void StopServer()
		{
			if (serverThread != null)
			{
				optionsForm.buttonActive.Text = "...";
				optionsForm.buttonActive.BackColor = Color.Moccasin;
				optionsForm.buttonActive.ForeColor = Color.Black;

				// tell the server thread that it needs to shut down...
				stopEvent.Set();

				// ...and wait for the thread to stop.
				serverThread.Join();

				serverThread = null;

				optionsForm.buttonActive.Text = "Paused";
				optionsForm.buttonActive.BackColor = Color.LightCoral;
				optionsForm.buttonActive.ForeColor = Color.White;
			}
		}

		public static bool ServerActive()
		{
			return (serverThread == null) ? false : true;
		}

		public static void UpdatePort(int value)
		{
			bool restart = ServerActive();

			if (restart)
				StopServer();

			Properties.Settings.Default.port = value;

			if (restart)
				StartServer();
		}

		public static void UpdateRootDir(string value)
		{
			bool restart = ServerActive();

			if (restart)
				StopServer();

			rootDir = value;

			if (restart)
				StartServer();
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

			stopEvent = new AutoResetEvent(false);
			servedEvent = new AutoResetEvent(false);

			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			optionsForm = new ServerOptionsForm();
			Application.Run(optionsForm);

			// We're closing now, so close the "single instance" system global mutex
			m.Close();
		}
	}
}
