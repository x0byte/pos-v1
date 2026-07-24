using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApp1
{
    public sealed class ReturnRequest
    {
        public int? OriginalBillId { get; set; }
        public string Cashier { get; set; }
        public string ApprovedBy { get; set; }
        public bool IsBillLinked => OriginalBillId.HasValue;
        public string RefundMethod { get; set; }
        public int CreditAccountId { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string ExchangeReference { get; set; }
        public string LocalTransactionId { get; set; }
        public List<ReturnItemRequest> Items { get; set; } = new List<ReturnItemRequest>();
    }

    public sealed class ReturnItemRequest
    {
        public int? OriginalBillItemId { get; set; }
        public int ProductId { get; set; }
        public string ItemName { get; set; }
        public decimal Quantity { get; set; }
        public decimal? OriginalUnitPrice { get; set; }
        public decimal EnteredUnitRefund { get; set; }
        public decimal RefundAmount { get; set; }
        public string Condition { get; set; }
        public bool Restock { get; set; }
        public string Notes { get; set; }
    }

    public sealed class ReturnResult
    {
        public long ReturnId { get; set; }
        public string ReturnReference { get; set; }
        public bool ExistingTransaction { get; set; }
    }

    public sealed class OriginalBillLookupResult
    {
        public DataRow Header { get; set; }
        public DataTable Items { get; set; }
    }
}
