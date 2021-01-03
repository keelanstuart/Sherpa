using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.IO;
using System.Net;
using Microsoft.Win32;

namespace Sherpa
{
	public class HttpServer
	{
		protected AutoResetEvent evShutdown;			// signaled when it's time to shut down
		protected AutoResetEvent evSettings;			// signaled to update the listener settings
		protected AutoResetEvent evStart;			// signaled to start the listener
		protected AutoResetEvent evStop;				// signaled to stop the listener
		protected AutoResetEvent evServed;			// signaled after each request has been served, reset when a request is received
		protected Mutex rootDirMutex;

		protected int requestCount;

		protected int port;
		protected string rootDir;
		protected string url;

		public delegate void logFuncType(string s);
		protected logFuncType logFunc;                // provide a callback so that the user can log data however they want

		public enum State { Active, Inactive, Waiting };
		protected State curState;
		public delegate void reportStateFuncType(State value);
		protected reportStateFuncType reportStateFunc;

		public const int defaultPort = 8000;
		public const string defaultRootDir = "./";

		private Thread serverThread;

		public HttpServer()
		{
			evShutdown = new AutoResetEvent(false);
			evSettings = new AutoResetEvent(true);
			evStart = new AutoResetEvent(false);
			evStop = new AutoResetEvent(false);
			evServed = new AutoResetEvent(false);

			rootDirMutex = new Mutex(false);

			requestCount = 0;

			port = defaultPort;
			rootDir = defaultRootDir;
			curState = State.Inactive;
			logFunc = null;
			reportStateFunc = null;

			serverThread = null;
		}

		public void Dispose()
		{
			StopServer();
			Thread.Sleep(0);
			evShutdown.Set();
			if (serverThread != null)
			{
				serverThread.Join();
				serverThread = null;
			}
		}

		public void SetLogFunc(logFuncType userLogFunc)
		{
			logFunc = userLogFunc;
		}

		public void SetReportStateFunc(reportStateFuncType userReportStateFunc)
		{
			reportStateFunc = userReportStateFunc;
			if (reportStateFunc != null)
				reportStateFunc(curState);
		}

		private void ListenerRequestCallback(IAsyncResult result)
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
			string filename = rootDir;
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
			if (logFunc != null)
				logFunc(string.Format("Request({0}): {2} << \"{1}\" ({3})\n", ++requestCount, req.Url.ToString(), req.UserHostName, filename));

			byte[] databuf = null;

			// if the file requested doesn't exist...
			if (!File.Exists(filename))
			{
				// The 404 message
				string fourOhFour = "<html>" +
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
			if (ext.ToLower() != ".js")
			{
				resp.ContentType = "application/octet-stream";
				RegistryKey rk = Registry.ClassesRoot.OpenSubKey(ext);
				if (rk != null)
				{
					object tmp_type = rk.GetValue("Content Type", "application/octet-stream");
					if (tmp_type != null)
						resp.ContentType = tmp_type.ToString();
				}
			}
			else
			{
				resp.ContentType = "text/javascript";
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
			evServed.Set();
		}

		public void ServerThreadProc()
		{
			WaitHandle[] waitEvents = new WaitHandle[5] { evShutdown, evSettings, evStart, evStop, evServed };

			// Create a Http server and start listening for incoming connections
			HttpListener listener = new HttpListener();

			IAsyncResult ctx = null;

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
								curState = State.Inactive;
								if (reportStateFunc != null)
									reportStateFunc(curState);

								listener.Stop();
								ctx.AsyncWaitHandle.WaitOne(-1);
								evServed.Reset();
							}

							// build the listener url string using the designated port.
							url = "http://localhost:";
							url += port.ToString();
							url += "/";

							listener.Prefixes.Clear();
							listener.Prefixes.Add(url);

							if (restart)
								evStart.Set();
							break;
						}

					case 2:     // start
						if (!listener.IsListening)
						{
							// start the listener...
							listener.Start();

							if (logFunc != null)
								logFunc(string.Format("Listening for connections as {0}\n", url));

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
								evStart.Set();
							}
						}

						curState = State.Active;
						if (reportStateFunc != null)
							reportStateFunc(curState);
						break;

					case 3:     // stop
						if (listener.IsListening)
						{
							listener.Stop();
							ctx.AsyncWaitHandle.WaitOne(-1);
						}

						curState = State.Inactive;
						if (reportStateFunc != null)
							reportStateFunc(curState);
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

		public void StartServer()
		{
			if (serverThread == null)
			{
				serverThread = new Thread(new ThreadStart(ServerThreadProc));
				serverThread.Start();
			}

			evStart.Set();
		}

		public void StopServer()
		{
			evStop.Set();
		}

		public void UpdatePort(int value)
		{
			port = value;
			evSettings.Set();
		}

		public void UpdateRootDir(string value)
		{
			if (value != rootDir)
			{
				rootDirMutex.WaitOne(-1);
				rootDir = value;
				rootDirMutex.ReleaseMutex();
			}

			if (logFunc != null)
				logFunc(string.Format("Serving content from file://{0}\n", value));
		}
		public string BaseRequestUrl()
		{
			return url;
		}
	}
}
