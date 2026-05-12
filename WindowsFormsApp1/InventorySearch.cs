using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApp1
{
    public static class InventorySearch
    {
        public static List<InventoryItem> GetMatches(string query, int limit)
        {
            string normalized = Normalize(query);
            string noSpace = normalized.Replace(" ", string.Empty);

            return AppCache.Inventory
                .Where(item => Matches(item, normalized, noSpace))
                .Take(limit)
                .ToList();
        }

        public static bool Matches(InventoryItem item, string query)
        {
            string normalized = Normalize(query);
            string noSpace = normalized.Replace(" ", string.Empty);
            return Matches(item, normalized, noSpace);
        }

        private static bool Matches(InventoryItem item, string normalized, string noSpace)
        {
            if (item == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return true;
            }

            string itemName = item.NormalizedItemName ?? string.Empty;
            string compactItemName = item.CompactNormalizedItemName ?? string.Empty;
            string keywords = item.NormalizedKeywords ?? string.Empty;

            return itemName.Contains(normalized) ||
                   compactItemName.Contains(noSpace) ||
                   keywords.Contains(normalized);
        }

        private static string Normalize(string text)
        {
            return (text ?? string.Empty).Trim().ToLowerInvariant();
        }
    }
}
