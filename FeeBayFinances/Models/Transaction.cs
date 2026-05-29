namespace FeeBayFinances.Models
{
    public class Transaction
    {
        public string TransactionId { get; set; }

        public string TransactionType { get; set; }

        public string Status { get; set; }

        public Amount Amount { get; set; }

        public Amount FeeAmount { get; set; }

        public string TransactionDate { get; set; }

        public OrderId OrderId { get; set; }
    }
}
