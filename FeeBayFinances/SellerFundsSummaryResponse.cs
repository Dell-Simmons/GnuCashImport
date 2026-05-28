using FeeBayFinances.Models;
using System;

namespace FeeBayFinances
{
    public class SellerFundsSummaryResponse
    {
        public Amount TotalFunds { get; set; }

        public Amount ProcessingFunds { get; set; }

        public Amount AvailableFunds { get; set; }

        public Amount FundsOnHold { get; set; }
    }
}