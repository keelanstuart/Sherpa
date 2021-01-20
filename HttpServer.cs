using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Net;
using Microsoft.Win32;
using System.Runtime.InteropServices;

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

		public HttpServer(int initial_port = defaultPort, string initial_rootdir = defaultRootDir)
		{
			evShutdown = new AutoResetEvent(false);
			evSettings = new AutoResetEvent(true);
			evStart = new AutoResetEvent(false);
			evStop = new AutoResetEvent(false);
			evServed = new AutoResetEvent(false);

			rootDirMutex = new Mutex(false);

			requestCount = 0;

			port = initial_port;
			rootDir = initial_rootdir;
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
			Stopwatch sw = Stopwatch.StartNew();
			sw.Start();

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

			++requestCount;

			// Print out some info about the request
			if (logFunc != null)
				logFunc(string.Format("({0}): {2} << \"{1}\" ({3})\n", requestCount, req.Url.ToString(), req.UserHostName, filename));

			// generic stream that will either pull data from a file or a generated string
			Stream datastream;
			long fulllength;

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
				string s = String.Format(fourOhFour, filename);

				// In this case, the datastream is a memory stream
				byte[] tmp = Encoding.UTF8.GetBytes(s);
				datastream = new MemoryStream(tmp);
				fulllength = tmp.Length;
			}
			else
			{
				// otherwise, load it's contents into data and report "OK"
				resp.StatusCode = 200;
				datastream = File.OpenRead(filename);

				fulllength = new System.IO.FileInfo(filename).Length;
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

			// avoid CORS issues with asking for data from somewhere else...
			resp.AddHeader("Access-Control-Allow-Origin", "*");

			// Send chunked if the data isn't "text" or it's larger than 256KB
			resp.SendChunked = true;// (data.LongLength >= (1 << 18)) || !resp.ContentType.Contains("text", StringComparison.CurrentCultureIgnoreCase);
			resp.ContentLength64 = fulllength;

			int read;
			// read chunks of the input datastream and write them to the response stream
			using (BinaryWriter bw = new BinaryWriter(resp.OutputStream))
			{
				// 2 x 1MB buffers
				const long bufsz = 1024 * 1024;
				byte[][] buffer = new byte[2][] { new byte[bufsz], new byte[bufsz] };

				int rbidx = 0;

				Task write_task = null;

				while ((read = datastream.Read(buffer[rbidx], 0, buffer[rbidx].Length)) > 0)
				{
					try
					{
						if (write_task != null)
							write_task.Wait();

						write_task = bw.BaseStream.WriteAsync(buffer[rbidx], 0, read);
					}
					catch (Exception)
					{
						break;
					}

					rbidx ^= 1;

					Thread.Sleep(0);
				}

				try
				{
					bw.Close();
				}
				catch (Exception)
				{

				}
			}

			resp.Close();
			datastream.Close();

			// Once the request has been served, we can quit if we need to
			evServed.Set();

			sw.Stop();
			if (logFunc != null)
				logFunc(string.Format("\t... served in {0}ms\n", sw.ElapsedMilliseconds.ToString()));
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
					{
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
					}

					case 3:     // stop
					{
						if (listener.IsListening)
						{
							listener.Stop();
							ctx.AsyncWaitHandle.WaitOne(-1);
						}

						curState = State.Inactive;
						if (reportStateFunc != null)
							reportStateFunc(curState);

						break;
					}

					case 4:     // serve completed, so refresh the context
					{
						if (listener.IsListening)
							ctx = listener.BeginGetContext(new AsyncCallback(ListenerRequestCallback), listener);

						break;
					}
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
