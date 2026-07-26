using FeeBayConnectionTester.DTO;
using SimpleFin;
using System;
using System.Linq;
using SimpleFin.SimpleFinDTO;
namespace FeeBayConnectionTester.Services.PeakCreditUnion
{
    public interface IPeakCUTransactionProcessor
    {
        Task<List<ToGnuCash>> ProcessPeakCuTransactionsAsync(List<SimpleFinTransaction> peakCuTransactions);
    }
}