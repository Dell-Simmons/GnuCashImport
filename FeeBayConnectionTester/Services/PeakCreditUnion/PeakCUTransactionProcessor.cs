using FeeBayConnectionTester.DTO;
using SimpleFin.SimpleFinDTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeeBayConnectionTester.Services.PeakCreditUnion
{
    public class PeakCUTransactionProcessor:IPeakCUTransactionProcessor
    {
        public async Task<List<ToGnuCash>> ProcessPeakCuTransactionsAsync(List<SimpleFinTransaction> peakCuTransactions)
        {
            return new List<ToGnuCash>();
        }
    }
}
