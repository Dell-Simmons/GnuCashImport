using FeeBayConnectionTester.DTO;
using SimpleFin;
using System;
using System.Linq;
using SimpleFin.SimpleFinDTO;
namespace FeeBayConnectionTester.Services.PeakCreditUnion
{
    public interface IPeakCUTransactionProcessor
    {
        Task<List<PeakCUToGnuCash>> ProcessPeakCuTransactionsAsync(List<SimpleFinTransaction> peakCuTransactions);
    }
}