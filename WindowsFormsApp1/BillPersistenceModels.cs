using System;
using System.Collections.Generic;

namespace WindowsFormsApp1
{
    public sealed class BillLineRecord
    {
        public string ItemName { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public decimal DiscountedPrice { get; set; }
    }

    public sealed class StockMovementRecord
    {
        public string BillReference { get; set; }
        public string ItemName { get; set; }
        public decimal QtyDelta { get; set; }
        public DateTime OccurredAt { get; set; }
        public string CreatedByUsername { get; set; }
        public string Note { get; set; }
    }

    public sealed class PausedCartRecord
    {
        public string Label { get; set; }
        public DateTime PausedAt { get; set; }
        public List<BillLineRecord> Items { get; set; } = new List<BillLineRecord>();
    }
}
