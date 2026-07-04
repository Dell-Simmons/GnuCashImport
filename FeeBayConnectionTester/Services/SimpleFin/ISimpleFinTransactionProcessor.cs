using FeeBayConnectionTester.DTO;
using SimpleFin;
using System;
using System.Linq;
using SimpleFin.SimpleFinDTO;
namespace FeeBayConnectionTester.Services.SimpleFin
{
    public interface ISimpleFinTransactionProcessor
    {
        Task<List<ToGnuCash>> ProcessPeakCuTransactionsAsync(List<SimpleFinTransaction> peakCuTransactions);
        Task<List<ToGnuCash>> ProcessSparkCCTransactionsAsync(List<SimpleFinTransaction> sparkCCTransactions);
    }
}