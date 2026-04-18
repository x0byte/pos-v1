using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public static class DatabaseConfig
    {
        private sealed class DbConfig
        {
            public string Server { get; set; }
            public string Port { get; set; }
            public string Database { get; set; }
            public string Uid { get; set; }
            public string Pwd { get; set; }
            public string OverridePassword { get; set; }
        }

        private static readonly string ConfigFilePath =
            Path.Combine(Application.StartupPath, "config.json");

        public static string ConnectionString { get; private set; } =
            "server=127.0.0.1;database=db_stc;uid=root;pwd=;";

        /// <summary>
        /// Password required to override a below-cost sale. Set via config.json (OverridePassword field).
        /// If not configured, overrides are not permitted until the admin sets it.
        /// </summary>
        public static string OverridePassword { get; private set; }

        public static void Load()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    return;
                }

                string json = File.ReadAllText(ConfigFilePath);
                DbConfig config = JsonConvert.DeserializeObject<DbConfig>(json);
                if (config == null)
                {
                    return;
                }

                ConnectionString = BuildConnectionString(
                    config.Server,
                    config.Port,
                    config.Database,
                    config.Uid,
                    config.Pwd
                );
                OverridePassword = config.OverridePassword;
            }
            catch
            {
                // Keep default connection string when config is invalid.
            }
        }

        public static void Save(string server, string port, string database, string uid, string pwd)
        {
            string finalServer = string.IsNullOrWhiteSpace(server) ? "127.0.0.1" : server.Trim();
            string finalPort = (port ?? string.Empty).Trim();
            string finalDatabase = string.IsNullOrWhiteSpace(database) ? "db_stc" : database.Trim();
            string finalUid = string.IsNullOrWhiteSpace(uid) ? "root" : uid.Trim();
            string finalPwd = pwd ?? string.Empty;

            ConnectionString = BuildConnectionString(finalServer, finalPort, finalDatabase, finalUid, finalPwd);

            var config = new DbConfig
            {
                Server = finalServer,
                Port = finalPort,
                Database = finalDatabase,
                Uid = finalUid,
                Pwd = finalPwd,
                OverridePassword = OverridePassword  // preserve existing value across DB config saves
            };

            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }

        public static Dictionary<string, string> ParseCurrent()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["server"] = "127.0.0.1",
                ["port"] = "",
                ["database"] = "db_stc",
                ["uid"] = "root",
                ["pwd"] = ""
            };

            string[] parts = ConnectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                string[] kv = part.Split(new[] { '=' }, 2);
                if (kv.Length == 2)
                {
                    result[kv[0].Trim()] = kv[1];
                }
            }

            return result;
        }

        private static string BuildConnectionString(string server, string port, string database, string uid, string pwd)
        {
            if (!string.IsNullOrWhiteSpace(port))
            {
                return $"server={server};port={port};database={database};uid={uid};pwd={pwd};";
            }

            return $"server={server};database={database};uid={uid};pwd={pwd};";
        }
    }
}
