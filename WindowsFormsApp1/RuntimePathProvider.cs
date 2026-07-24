using System;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public static class RuntimePathProvider
    {
        private static readonly object SyncRoot = new object();
        private static string dataDirectory;

        public static string DataDirectory
        {
            get
            {
                EnsureDataDirectory();
                return dataDirectory;
            }
        }

        public static string GetDataFilePath(string fileName, bool migrateFromStartupPath = true)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name is required.", nameof(fileName));
            }

            EnsureDataDirectory();
            string targetPath = Path.Combine(dataDirectory, fileName);
            if (migrateFromStartupPath)
            {
                MigrateFile(fileName, targetPath);
            }

            return targetPath;
        }

        public static string GetDataDirectoryPath(string directoryName)
        {
            if (string.IsNullOrWhiteSpace(directoryName))
            {
                throw new ArgumentException("Directory name is required.", nameof(directoryName));
            }

            EnsureDataDirectory();
            string path = Path.Combine(dataDirectory, directoryName);
            Directory.CreateDirectory(path);
            MigrateDirectory(directoryName, path);
            return path;
        }

        private static void EnsureDataDirectory()
        {
            if (dataDirectory != null)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (dataDirectory != null)
                {
                    return;
                }

                string basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                if (string.IsNullOrWhiteSpace(basePath))
                {
                    basePath = Application.StartupPath;
                }

                dataDirectory = Path.Combine(basePath, "Saman Trade Center", "STC POS");
                Directory.CreateDirectory(dataDirectory);
            }
        }

        private static void MigrateFile(string fileName, string targetPath)
        {
            try
            {
                if (File.Exists(targetPath))
                {
                    return;
                }

                string sourcePath = Path.Combine(Application.StartupPath, fileName);
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, targetPath, false);
                }
            }
            catch
            {
                // The app can continue using the target path even if migration is blocked.
            }
        }

        private static void MigrateDirectory(string directoryName, string targetPath)
        {
            try
            {
                string sourcePath = Path.Combine(Application.StartupPath, directoryName);
                if (!Directory.Exists(sourcePath) || string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                foreach (string sourceFile in Directory.GetFiles(sourcePath))
                {
                    string targetFile = Path.Combine(targetPath, Path.GetFileName(sourceFile));
                    if (!File.Exists(targetFile))
                    {
                        File.Copy(sourceFile, targetFile, false);
                    }
                }
            }
            catch
            {
                // Best-effort migration only.
            }
        }
    }
}
