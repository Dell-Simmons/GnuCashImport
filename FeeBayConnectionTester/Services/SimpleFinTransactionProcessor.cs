using EbaySharp.Entities.Develop.SellingApps.OrderManagement.Fulfillment.Order;
using FeeBayConnectionTester;
using FeeBayConnectionTester.Extensions;
using LocalDBConnections;
using SimpleFin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace FeeBayConnectionTester.Services
{       
    public interface ISimpleFinTransactionProcessor
    {
        Task<List<ToGnuCash>> ProcessPeakCuTransactionsAsync(List<SimpleFinTransaction> peakCuTransactions);
        Task<List<ToGnuCash>> ProcessSparkCCTransactionsAsync(List<SimpleFinTransaction> sparkCCTransactions);
    }
    public class SimpleFinTransactionProcessor : ISimpleFinTransactionProcessor
    {
        public async Task<List<ToGnuCash>> ProcessPeakCuTransactionsAsync(List<SimpleFinTransaction> peakCuTransactions)
        {
            return new List<ToGnuCash>();
        }
        public async Task<List<ToGnuCash>> ProcessSparkCCTransactionsAsync(List<SimpleFinTransaction> sparkCCTransactions)
        {
            return new List<ToGnuCash>();
        }
    }
}