using System.Collections.Generic;

namespace FeeBayFinances.Models
{
    public class GetTransactionsResponse
    {
        public List<Transaction> Transactions { get; set; }

        public int Total { get; set; }

        public string Href { get; set; }

        public string Next { get; set; }

        public string Prev { get; set; }
    }
}
