using System;
using System.Collections.Generic;
using System.Text;

namespace FeeBayConnectionTester
{
    public class ToGnuCash
    {
        public DateTime Date { get; set; }
        public string Account { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string TransactionId { get; set; }
        public int SortOrder { get; set; }  
    }
}
