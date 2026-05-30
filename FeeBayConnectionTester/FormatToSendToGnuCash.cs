using EbaySharp.Entities.Common;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances.Transaction;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeeBayConnectionTester
{
    internal class FormatToSendToGnuCash
    {
        internal FormatToSendToGnuCash(List<Transaction> transactions)
        {
            var sales = transactions.Where(x => x.TransactionType.Equals("SALE", StringComparison.CurrentCultureIgnoreCase));
            var refund = transactions.Where(x => x.TransactionType.Equals("REFUND", StringComparison.CurrentCultureIgnoreCase));
            var credit = transactions.Where(x => x.TransactionType.Equals("CREDIT", StringComparison.CurrentCultureIgnoreCase));
            var transfer = transactions.Where(x => x.TransactionType.Equals("TRANSFER", StringComparison.CurrentCultureIgnoreCase));

            var totalTransactions = transactions.Count();
            var salesTransactions = sales.Count();
            var refundTransactions = refund.Count();
            var creditTransactions = credit.Count();
            var transferTransactions = transfer.Count();

        }



            // var tempOrders = incomingRecords.Where(x => x.Type.Equals("order", StringComparison.CurrentCultureIgnoreCase));
            // some tempOrders have more than one item per order . . . 
            //var orders = from record in tempOrders group record by record.Order_number into newGroup select newGroup;
            //var refunds = incomingRecords.Where(x => x.Type.Equals("refund", StringComparison.CurrentCultureIgnoreCase));
            //var claims = incomingRecords.Where(x => x.Type.Equals("claim", StringComparison.CurrentCultureIgnoreCase));
            //var disputes = incomingRecords.Where(x => x.Type.Equals("payment dispute", StringComparison.CurrentCultureIgnoreCase));
            //var shippingLabels = incomingRecords.Where(x => x.Type.Equals("shipping label", StringComparison.CurrentCultureIgnoreCase));
            //var charges = incomingRecords.Where(x => x.Type.Equals("charge", StringComparison.CurrentCultureIgnoreCase));
            //var transfers = incomingRecords.Where(x => x.Type.Equals("transfer", StringComparison.CurrentCultureIgnoreCase));
            //var holds = incomingRecords.Where(x => x.Type.Equals("hold", StringComparison.CurrentCultureIgnoreCase));
            //var otherFees = incomingRecords.Where(x => x.Type.Equals("other fee", StringComparison.CurrentCultureIgnoreCase));
            //var adjustments = incomingRecords.Where(x => x.Type.Equals("adjustment", StringComparison.CurrentCultureIgnoreCase));
            //var purchases = incomingRecords.Where(x => x.Type.Equals("purchase", StringComparison.CurrentCultureIgnoreCase));
            //var payouts = incomingRecords.Where(x => x.Type.Equals("payout", StringComparison.CurrentCultureIgnoreCase));
            //var secondaryPayouts = incomingRecords.Where(x => x.Type.Equals("secondary payout", StringComparison.CurrentCultureIgnoreCase));
            //var withheldTaxes = incomingRecords.Where(x => x.Type.Equals("withheld tax", StringComparison.CurrentCultureIgnoreCase));
            //var reserves = incomingRecords.Where(x => x.Type.Equals("reserve", StringComparison.CurrentCultureIgnoreCase));



            //    foreach (var transaction in transactions)
            //    {
            //        string transactionId = transaction.TransactionId;
            //        string transactionDate = transaction.TransactionDate;
            //        Amount amount = transaction.Amount;
            //        string orderId = transaction.OrderId;
            //    TransactionStatusEnum? transactionStatus = transaction.TransactionStatus;
            //    TransactionTypeEnum? transactionType = transaction.TransactionType;
            //    FeeTypeEnum? feeType = transaction.FeeType;
            //    var taxes = transaction.EBayCollectedTaxAmount;
            //    if (transactionType == TransactionTypeEnum.SALE)
            //    {
            //        var buyer = transaction.Buyer;
            //        foreach (OrderLineItem lineItem in transaction.OrderLineItems)
            //        {
            //            var donations = lineItem.Donations;
            //            var feeBasisAmount = lineItem.FeeBasisAmount;
            //            var lineItemId = lineItem.LineItemId;
            //            var marketplaceFees = lineItem.MarketplaceFees;
            //        }
            //    }
            //}
        //}
    }
