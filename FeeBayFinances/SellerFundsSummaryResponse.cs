using FeeBayFinances.Models;
using System;

namespace FeeBayFinances
{
    public class SellerFundsSummaryResponse
    {
        public Amount CurrentBalance { get; set; }

        public Amount AvailableFunds { get; set; }

        public Amount TotalFundsOnHold { get; set; }

        public Amount PayoutsBeingProcessed { get; set; }
    }
}