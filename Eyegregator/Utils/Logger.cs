using System;
using Java.IO;
using System.IO;
using Android.App;
using Android.Widget;
using Android.Util;

namespace Eyegregator
{
	public class Logger
	{
		private const string logFilePath = "/sdcard/AppLogger";
		private const string logFileName = "Eyegregator.txt";
		public static BufferedWriter outWriter;

		public static void LogThis (string message, string provider, string classToLog)
		{
			try {
				string filePath = Path.Combine (logFilePath, logFileName);
				using (var file = System.IO.File.Open(filePath, FileMode.Append, FileAccess.Write)) {
					using (var strm = new StreamWriter(file)) {
						strm.WriteLine (DateTime.Now.ToString() + ": Class: " +classToLog + "; Method: " + provider + "; Exception: " +message);
						strm.WriteLine (Environment.NewLine);
						strm.Flush ();
						strm.Close ();
					}
				}
			} catch (Exception ex) {
				Log.Debug (provider, message + Environment.NewLine + ex.Message);
			}
		}
	}
}

