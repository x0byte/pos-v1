using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public static class DesktopPosLocalAudit
    {
        private static readonly string OverrideLogPath = RuntimePathProvider.GetDataFilePath("override_log.csv");
        private static readonly string SnapshotDirectory = RuntimePathProvider.GetDataDirectoryPath("mobile_bill_snapshots");

        public static void AppendOverrideLog(string username, string itemName, decimal qty, decimal retailPrice, decimal cost, decimal attemptedLineTotal, string reasonSnippet)
        {
            try
            {
                bool writeHeader = !File.Exists(OverrideLogPath);
                using (StreamWriter writer = new StreamWriter(OverrideLogPath, true))
                {
                    if (writeHeader)
                    {
                        writer.WriteLine("timestamp,username,item_name,qty,retail_price,cost,attempted_line_total,reason_snippet");
                    }

                    writer.WriteLine(ToCsvLine(new[]
                    {
                        DateTime.Now.ToString("o"),
                        username ?? string.Empty,
                        itemName ?? string.Empty,
                        qty.ToString(CultureInfo.InvariantCulture),
                        retailPrice.ToString(CultureInfo.InvariantCulture),
                        cost.ToString(CultureInfo.InvariantCulture),
                        attemptedLineTotal.ToString(CultureInfo.InvariantCulture),
                        reasonSnippet ?? string.Empty
                    }));
                }
            }
            catch
            {
                // Local audit only. Do not block cashier flow.
            }
        }

        public static string SavePendingSnapshot(string sessionId, IEnumerable<PendingBillSnapshotLine> items)
        {
            try
            {
                string safeSessionId = SanitizeSessionId(sessionId);
                EnsureSnapshotDirectory();

                string path = Path.Combine(SnapshotDirectory, safeSessionId + ".json");
                string json = JsonConvert.SerializeObject(items?.ToList() ?? new List<PendingBillSnapshotLine>(), Formatting.Indented);
                File.WriteAllText(path, json);
                return path;
            }
            catch
            {
                return null;
            }
        }

        public static void WritePendingDiff(string sessionId, IEnumerable<PendingBillSnapshotLine> finalItems)
        {
            try
            {
                string safeSessionId = SanitizeSessionId(sessionId);
                EnsureSnapshotDirectory();

                string snapshotPath = Path.Combine(SnapshotDirectory, safeSessionId + ".json");
                string diffPath = Path.Combine(SnapshotDirectory, safeSessionId + ".diff.json");
                List<PendingBillSnapshotLine> originalItems = File.Exists(snapshotPath)
                    ? JsonConvert.DeserializeObject<List<PendingBillSnapshotLine>>(File.ReadAllText(snapshotPath)) ?? new List<PendingBillSnapshotLine>()
                    : new List<PendingBillSnapshotLine>();
                List<PendingBillSnapshotLine> finalList = finalItems?.ToList() ?? new List<PendingBillSnapshotLine>();

                List<object> added = new List<object>();
                List<object> removed = new List<object>();
                List<object> modified = new List<object>();
                int max = Math.Max(originalItems.Count, finalList.Count);

                for (int i = 0; i < max; i++)
                {
                    PendingBillSnapshotLine original = i < originalItems.Count ? originalItems[i] : null;
                    PendingBillSnapshotLine current = i < finalList.Count ? finalList[i] : null;

                    if (original == null && current != null)
                    {
                        added.Add(new { index = i, line = current });
                        continue;
                    }

                    if (original != null && current == null)
                    {
                        removed.Add(new { index = i, line = original });
                        continue;
                    }

                    if (!AreEqual(original, current))
                    {
                        modified.Add(new { index = i, original, current });
                    }
                }

                string json = JsonConvert.SerializeObject(new
                {
                    session_id = safeSessionId,
                    generated_at = DateTime.Now,
                    added,
                    removed,
                    modified
                }, Formatting.Indented);

                File.WriteAllText(diffPath, json);
            }
            catch
            {
                // Local audit only. Do not block checkout.
            }
        }

        private static bool AreEqual(PendingBillSnapshotLine left, PendingBillSnapshotLine right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            return string.Equals(left.ItemName ?? string.Empty, right.ItemName ?? string.Empty, StringComparison.Ordinal)
                && left.Rate == right.Rate
                && left.Amount == right.Amount
                && left.DiscountedPrice == right.DiscountedPrice;
        }

        private static void EnsureSnapshotDirectory()
        {
            if (!Directory.Exists(SnapshotDirectory))
            {
                Directory.CreateDirectory(SnapshotDirectory);
            }
        }

        private static string SanitizeSessionId(string sessionId)
        {
            string value = string.IsNullOrWhiteSpace(sessionId) ? "unknown-session" : sessionId.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value;
        }

        private static string ToCsvLine(string[] values)
        {
            return string.Join(",", values.Select(EscapeCsv));
        }

        private static string EscapeCsv(string input)
        {
            string value = input ?? string.Empty;
            if (value.Contains("\""))
            {
                value = value.Replace("\"", "\"\"");
            }

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value + "\"";
            }

            return value;
        }
    }

    public sealed class PendingBillSnapshotLine
    {
        public string ItemName { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public decimal DiscountedPrice { get; set; }
    }
}
