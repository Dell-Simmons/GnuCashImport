using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances.Payout;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances.Transaction;
using EbaySharp.Entities.Develop.SellingApps.OrderManagement.Fulfillment.Order;
using FeeBayConnectionTester.DTO;
using System;
using System.Linq;

namespace FeeBayConnectionTester.Services.FeeBay
{
    /// <summary>
    /// Processes eBay transaction data and converts it to GnuCash format.
    /// Handles different transaction types (sales, refunds, payouts, fees, etc.)
    /// </summary>
    public interface IFeeBayTransactionProcessor
    {
        Task<List<ToGnuCash>> ProcessTransactionsAsync(
            List<Payout> payoutList,
            List<Transaction> transactionList,
            List<Order> orderList);
    }
}
