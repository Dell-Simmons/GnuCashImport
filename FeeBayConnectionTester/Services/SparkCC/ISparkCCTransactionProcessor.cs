using FeeBayConnectionTester.DTO;
using SimpleFin;
using System;
using System.Linq;
using SimpleFin.SimpleFinDTO;
namespace FeeBayConnectionTester.Services.SimpleFin
{
    public interface ISparkCCTransactionProcessor
    {
        Task<List<ToGnuCash>> ProcessSparkCCTransactionsAsync(List<SimpleFinTransaction> sparkCCTransactions);
    }
}