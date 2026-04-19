namespace SimplePOS
{
    public class Bill
    {
        public string BillId { get; set; }
        public string KoTNumber { get; set; } // Added property for KOT number
        public string Date { get; set; }
        public string Time { get; set; }
        public string Items { get; set; }
        public double TotalAmount { get; set; }
    }
}