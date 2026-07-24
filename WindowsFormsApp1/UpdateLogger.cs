using System;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public static class UpdateLogger
    {
        private static readonly object SyncRoot = new object();
        private static readonly string LogPath = RuntimePathProvider.GetDataFilePath("updates.log");

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Error(string message, Exception ex = null)
        {
            Write("ERROR", ex == null ? message : message + " " + ex.Message);
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (SyncRoot)
                {
                    File.AppendAllText(LogPath,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [" + level + "] " + message + Environment.NewLine);
                }
            }
            catch
            {
                // Logging must never interrupt POS operation.
            }
        }
    }
}
