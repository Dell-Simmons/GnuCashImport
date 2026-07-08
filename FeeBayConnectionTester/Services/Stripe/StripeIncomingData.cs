namespace GnuCashCSVImporter.IncomingModels.Stripe
{
    public class StripeIncomingData
    {
        public required string Id { get; set; }
        public required string Type { get; set; }
        public required string Source { get; set; }
        public required decimal Amount { get; set; }
        public required decimal Fee { get; set; }
        public decimal Destination_Platform_Fee { get; set; }
        public decimal Destination_Platform_Fee_Currency { get; set; }
        public required decimal Net { get; set; }
        public required string Currency { get; set; }
        public required DateTime Created { get; set; }
        public required DateTime Available_On { get; set; }
        public required string Description { get; set; }
        public decimal Customer_Facing_Amount { get; set; }
        public required string Customer_Facing_Currency { get; set; }
        public required string Transfer { get; set; }
        public required DateTime Transfer_Date { get; set; }
        public required string Transfer_Group { get; set; }
        public required string NopCommerce_Order_GUID { get; set; }
        public required string OrderGuid { get; set; }
    }
}
