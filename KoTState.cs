namespace SimplePOS
{
    // Holds the most recently printed KOT (for KOT+Bill) so reprints can reuse the same KOT number/data.
    public static class KoTState
    {
        public static string LastKoTNumber { get; set; } = string.Empty;
        public static string LastKoTItems { get; set; } = string.Empty;
        public static string LastKoTInstructions { get; set; } = string.Empty;
        public static string LastBillId { get; set; } = string.Empty;
    }
}
