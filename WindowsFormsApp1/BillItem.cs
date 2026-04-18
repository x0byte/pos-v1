namespace WindowsFormsApp1
{
    public class BillItem
    {
        public int RowId { get; set; }
        public string ItemName { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public decimal DiscountedPrice { get; set; }
    }
}
