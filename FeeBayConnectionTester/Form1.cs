using EbaySharp.Controllers;
using EbaySharp.Entities.Common;
using EbaySharp.Entities.Develop.KeyManagement.SigningKey;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances.Payout;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances.Transaction;
using EbaySharp.Entities.Develop.SellingApps.OrderManagement.Fulfillment.Order;
using FeeBayConnectionTester.Extensions;
using FeeBayConnectionTester.Services;
using FeeBayOAuth.TokenService;
using LocalDBConnections;
using LocalDBConnections.StampDataDB.StampDataEntities;
using MicroOrm.Dapper.Repositories.SqlGenerator.Filters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FeeBayConnectionTester
{
    public partial class Form1 : Form
    {
        #region Constants and Fields
        private readonly Func<string, EbayController> _ebayControllerFactory;
        private readonly ILocalDbConnectionManager _localDbConnectionManager;
        private readonly IOAuthTokenService _oAuthTokenService;
        
        private EbayController _eBayController = null!;
        #endregion

        #region Constructors
        public Form1(
            IOAuthTokenService oAuthTokenFactory,
            ILocalDbConnectionManager localDbConnectionManager,
            Func<string, EbayController> ebayControllerFactory)
        {
            InitializeComponent();
            _oAuthTokenService = oAuthTokenFactory;
            _localDbConnectionManager = localDbConnectionManager;
            _ebayControllerFactory = ebayControllerFactory;
        }
        #endregion

        #region Event handlers
        #region Button1 Click Workflow
        private async void button1_Click(object sender, EventArgs e)
        {
            // token identifies the user and application,
            // and is used to authenticate API requests.
            // It is typically obtained through an OAuth flow,
            // where the user grants permission for the application to access their eBay data.
            // The token is then included in the Authorization header of API requests
            // to verify the identity of the requester and ensure they have the necessary
            // permissions to perform the requested actions.
            string? token = await _oAuthTokenService.GetOAuthTokenAsync("Simmons_Ink");
            if (string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show(
                    "Unable to acquire an OAuth token for Simmons_Ink.",
                    "Authentication Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            _eBayController = _ebayControllerFactory(token);
            // The signing key is used to create digital signatures for API requests,
            // it is associated with the application
            // and is used to ensure the integrity and authenticity of the requests.
            // The one stored in the database is good for 3 years from today (5/29/26).
            // So don't fucking worry about it expiring anytime soon.
            var signingKey = await GetOrCreateSigningKey(_eBayController);

            string multiFilter;

            // Combine multiple filters into ONE comma-separated string

            //!{PAYOUT} is funds going from feeBay to bank account
            // multiFilter = "transactionStatus:{PAYOUT},transactionDate:[2026-01-01T00:00:00.000Z..2026-01-31T23:59:59.000Z]";

            //TransactionSummary transactionPayoutSummary = 
            //    await ebayController.GetTransactionSummary(signingKey, multiFilter);
            ////!{ COMPLETED} is funds going from buyer to feeBay.
            //multiFilter = "transactionStatus:{COMPLETED},transactionDate:[2026-01-01T00:00:00.000Z..2026-01-31T23:59:59.000Z]";

            //!Get Payouts (transfers from feeBay to checking from someplace
            //!Extend the Payouts filter by a week to catch payouts from end of month sales
            string payOutsFilter = "payoutDate:[2026-01-01T00:00:00.000Z..2026-02-14T23:59:59.999Z]";
            List<Payout> payOutList = await GetAllPayOutsPaginated(payOutsFilter, limit: 50);

            //!GetTransactions with pagination
            multiFilter = "transactionDate:[2025-12-25T00:00:00.000Z..2026-01-31T23:59:59.000Z]";
            List<Transaction> transactionList = await GetAllTransactionsPaginated(multiFilter, limit: 50);

            //!GetOrders with pagination
            string ordersFilter = "creationdate:[2025-12-25T00:00:00.000Z..2026-01-31T23:59:59.999Z]";
            List<Order> orderList = await GetAllOrdersPaginated(ordersFilter, limit: 50);

            List<FeeBayIncomingData> feeBayIncomingData = CombineDownloadedData(payOutList, transactionList, orderList);
            var incomingTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var incomingOutputPath = $@"D:\Exports\eBay_IncomingData_{incomingTimestamp}.csv";
            CsvExporter.WriteIncomingDataToCsv(feeBayIncomingData, incomingOutputPath);
            MessageBox.Show(
                $"Successfully exported {feeBayIncomingData.Count} incoming rows to:\n\n{incomingOutputPath}",
                "Incoming Data Export Successful",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            // await FormatToSendToGnuCash(orderList, financialTransactionList, payOutList);;
        }

        private List<FeeBayIncomingData> CombineDownloadedData(List<Payout> payOutList, List<Transaction> transactionList, List<Order> orderList)
        {
            var results = new List<FeeBayIncomingData>();
//! just as a check look for transactions not associated with any payout
            var transactionsWithoutPayout = transactionList
                .Where(t => string.IsNullOrWhiteSpace(t.PayoutId))
                .ToList();
            if (transactionsWithoutPayout.Any())
            {
                MessageBox.Show(
                    $"Found {transactionsWithoutPayout.Count} transactions not associated with any payout.",
                    "Transactions Without Payout",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }



            var payoutsById = payOutList
                .Where(p => string.IsNullOrWhiteSpace(p.PayoutId) == false)
                .GroupBy(p => p.PayoutId)
                .ToDictionary(g => g.Key, g => g.First());

          //  var ohioOrderInOrders = orderList.Where(o => o.OrderId == "09-14052-99669");
          //  var ohioOrderInTransactions = transactionList.Where(o => o.OrderId == "09-14052-99669");
            var groupedTransactions = transactionList
                .Where(t => string.IsNullOrWhiteSpace(t.PayoutId) == false)
                .GroupBy(t => t.PayoutId!)
                .ToDictionary(g => g.Key, g => g.ToList());
           // var ohioOrderInPayouts = payOutList.Where(o => o.)
            //! rely on transactions not orders.  Use orders only to get at orderItem
            //! specifics not available in Transaction orderItems.  Like title and SKU
            //! that should be about it.  Otherwise pull from transactions!
            foreach (var payout in payOutList)
            {
                var payoutId = payout.PayoutId;
                int? numTransactionsInPayout = payout.TransactionCount;
                List<Transaction>? transactionsInThisPayout;
                groupedTransactions.TryGetValue(payoutId, out transactionsInThisPayout);
                if (transactionsInThisPayout == null)
                {
                    continue;
                }
                if(transactionsInThisPayout.Count != numTransactionsInPayout)
                {
                    //! only try to reconcile transactions that are in a completed payout
                    continue;
                }
//!                }
//! no i think some transactions like store fees and others not associated with an order might be missing
//!
                foreach(var transaction in transactionsInThisPayout)
                {
                    var associatedOrder = orderList.Where(o=>o.OrderId == transaction.OrderId).FirstOrDefault();
                    if (associatedOrder == null)
                    {
                       // continue;
                    }
                    switch (transaction.TransactionType)
                    {
                        case TransactionTypeEnum.SALE:
                        HandleSale(transaction,associatedOrder);
                        break;
                        case TransactionTypeEnum.REFUND:
                        HandleRefund(transaction,associatedOrder);
                        break;
                        case TransactionTypeEnum.CREDIT:
                        HandleCredit(transaction,associatedOrder);
                        break;
                        case TransactionTypeEnum.DISPUTE:
                        HandleDispute(transaction,associatedOrder);
                        break;
                        case TransactionTypeEnum.SHIPPING_LABEL:
                        HandleShipping_Label(transaction,associatedOrder);
                        break;
                        case TransactionTypeEnum.TRANSFER:
                        HanedleTransfer(transaction,associatedOrder);
                        break;
                        case TransactionTypeEnum.NON_SALE_CHARGE:
                        HandleNon_Sale_Charge(transaction);
                        break;
                        case TransactionTypeEnum.ADJUSTMENT:
                        HandleAdjustment(transaction,associatedOrder);
                        break;
                        case TransactionTypeEnum.WITHDRAWAL:
                        HandleWithdrawal(transaction,associatedOrder);
                        break;
                        case TransactionTypeEnum.LOAN_REPAYMENT:
                        HandleLoan_Repayment(transaction,associatedOrder);
                        break;
                        case TransactionTypeEnum.PURCHASE:
                        HandlePurchase(transaction,associatedOrder);
                        break;
                        case null:
                        default:
                            continue;
                    }

               
                }
            }
            return results;
        }

        private void HandlePurchase(Transaction transaction, Order associatedOrder)
        {
           // throw new NotImplementedException();
        }

        private void HandleLoan_Repayment(Transaction transaction, Order associatedOrder)
        {
           // throw new NotImplementedException();
        }

        private void HandleWithdrawal(Transaction transaction, Order associatedOrder)
        {
           // throw new NotImplementedException();
        }

        private void HandleAdjustment(Transaction transaction, Order associatedOrder)
        {
            // throw new NotImplementedException();
        }

        private void HandleNon_Sale_Charge(Transaction transaction)
        {
            //! figure out what type of fee this is
            var feeTypes = transaction.References;
            if(feeTypes == null)
            {
                //!must be a store subscription charge
                HandleStoreSubscriptionCharge(transaction);
            }  
            foreach(var fee in feeTypes)
            {
                switch(fee.ReferenceType,transaction.TransactionMemo)
                {
                    case (ReferenceTypeEnum.ITEM_ID, "Promoted Listings - Priority fee"):
                        HandlePromotedListingsPriorityFees(transaction,fee.ReferenceId);
                        break;
                    default:
                    throw new NotImplementedException();
                        break;
                }
           

            }

               
        }

        private void HandleStoreSubscriptionCharge(Transaction transaction)
        {
            //   var otherFeeLineFrom = new Stripe.StripeModels.OutputData();
            //         otherFeeLineFrom.Date = DateOnly.FromDateTime(DateTime.Parse(otherFee.Transaction_creation_date));
            //         otherFeeLineFrom.Account = $"Assets:Current Assets:feeBay:{feeBayName2}";
            //         otherFeeLineFrom.Description = otherFee.Description;
            //         otherFeeLineFrom.Amount = -decimal.Parse(otherFee.Net_amount);
            //         otherFeeLineFrom.TransactionId = otherFee.Reference_ID;
            //         otherFeeLineFrom.SortOrder = 1;
            //         outputData.Add(otherFeeLineFrom);

            //         var otherFeeLineTo = new Stripe.StripeModels.OutputData();
            //         otherFeeLineTo.Date = DateOnly.FromDateTime(DateTime.Parse(otherFee.Transaction_creation_date));
            //         otherFeeLineTo.Account = $"Expenses:FeeBay Fees:{feeBayName1}:Store Monthly Fee";
            //         otherFeeLineTo.Description = string.Empty; //  payout.Description;
            //         otherFeeLineTo.Amount = decimal.Parse(otherFee.Net_amount);
            //         otherFeeLineTo.TransactionId = otherFee.Reference_ID;
            //         otherFeeLineTo.SortOrder = 2;
            //         outputData.Add(otherFeeLineTo);
            //throw new NotImplementedException();
        }

        private void HandlePromotedListingsPriorityFees(Transaction transaction,string itemId)
        {
            // Implement handling of Promoted Listings - Priority fees here
            var feeBaySellerID = "Simmons Ink";
              var outputData = new List<ToGnuCash>();

             var otherFeeLineFrom = new ToGnuCash();
                     otherFeeLineFrom.Date = DateOnly.FromDateTime(DateTime.Parse(transaction.TransactionDate));
                     otherFeeLineFrom.Account = $"Assets:Current Assets:feeBay:{feeBaySellerID}";
                     otherFeeLineFrom.Description = $"{transaction.TransactionMemo} ItemId {itemId}";
                     otherFeeLineFrom.Amount = -transaction.Amount.DollarAmount() ?? 0m;
                     otherFeeLineFrom.TransactionId = transaction.TransactionId;
                     otherFeeLineFrom.SortOrder = 1;
                     outputData.Add(otherFeeLineFrom);

                    var otherFeeLineTo = new ToGnuCash();
                    otherFeeLineTo.Date = DateOnly.FromDateTime(DateTime.Parse(transaction.TransactionDate));
                    otherFeeLineTo.Account = $"Expenses:FeeBay Fees:{feeBaySellerID}:Promoted Listings Fee";
                    otherFeeLineTo.Description = string.Empty; //  payout.Description;
                    otherFeeLineTo.Amount = transaction.Amount.DollarAmount() ?? 0m;
                    otherFeeLineTo.TransactionId = transaction.TransactionId;
                    otherFeeLineTo.SortOrder = 2;
                    outputData.Add(otherFeeLineTo); 
                }
        private void HanedleTransfer(Transaction transaction, Order associatedOrder)
        {
            //throw new NotImplementedException();
        }

        private void HandleShipping_Label(Transaction label, Order associatedOrder)
        {
            var feeBaySellerID = "Simmons Ink";
            var outputData = new List<ToGnuCash>();
                    var labelLineFrom = new ToGnuCash();
                    labelLineFrom.Date = DateOnly.FromDateTime(DateTime.Parse(label.TransactionDate));
                    labelLineFrom.Account = $"Assets:Current Assets:feeBay:{feeBaySellerID}";
                    labelLineFrom.Description = label.TransactionMemo;
                    labelLineFrom.Amount = -(label.Amount.DollarAmount() ?? 0m);
                    labelLineFrom.TransactionId = label.TransactionId;
                    labelLineFrom.SortOrder = 1;
                    outputData.Add(labelLineFrom);

                    var labelLineTo = new ToGnuCash();
                    labelLineTo.Date = DateOnly.FromDateTime(DateTime.Parse(label.TransactionDate));
                    labelLineTo.Account = $"Expenses:Postage and Delivery";
                    labelLineTo.Description = string.Empty; //  payout.Description;
                    labelLineTo.Amount = label.Amount.DollarAmount() ?? 0m;
                    labelLineTo.TransactionId = label.TransactionId;
                    labelLineTo.SortOrder = 2;
                    outputData.Add(labelLineTo);
                
        }

        private void HandleDispute(Transaction transaction, Order associatedOrder)
        {
           // throw new NotImplementedException();
        }

        private void HandleCredit(Transaction transaction, Order associatedOrder)
        {
            //throw new NotImplementedException();
        }

        private void HandleRefund(Transaction transaction, Order associatedOrder)
        {
             // this works leave it alone
                //! Consider inputing all csv numbers as absolute values . . .
                //! Then can use + or - as needed without worrying about positives or negatives in input
                //
                try
                {
                    // var cogs = _db.GetStampCostById(int.Parse(refund.Sku));
                    // var refundDate = refund.Payout_date;
                    // var orderId = refund.Order_number;
                    // var sellingPriceRefunded = Decimal.Parse(refund.Net_amount);
                    // var fixedFeesRefunded = Decimal.Parse(refund.FVF_fixed);
                    // var variableFeesRefunded = Decimal.Parse(refund.FVF_variable);
                    // var netRefund = Decimal.Parse(refund.Net_amount);
                    // var internationalFeesRefunded = Decimal.Parse(refund.International_fee);
                    // var grossRefund = Decimal.Parse(refund.Gross_transaction_amount);

                    // income line
                    // var incomeLine = new Stripe.StripeModels.OutputData();
                    // incomeLine.Date = DateOnly.FromDateTime(DateTime.Parse(refundDate));
                    // incomeLine.Account = $"Income:{feeBayName2} Sales";
                    // incomeLine.Description = $"feeBay Order #{orderId} - {refund.Item_title} REFUNDED";
                    // incomeLine.Amount = grossRefund;// + shippingPrice;
                    // incomeLine.TransactionId = orderId;
                    // incomeLine.SortOrder = 1;
                    // outputData.Add(incomeLine);

                    // feeBay current assett
                    // var netIncomeLine = new Stripe.StripeModels.OutputData();
                    // netIncomeLine.Date = DateOnly.FromDateTime(DateTime.Parse(refundDate));
                    // netIncomeLine.Account = $"Assets:Current Assets:feeBay:{feeBayName2}";
                    // netIncomeLine.Description = string.Empty;
                    // netIncomeLine.Amount = -netRefund;
                    // netIncomeLine.TransactionId = orderId;
                    // netIncomeLine.SortOrder = 2;
                    // outputData.Add(netIncomeLine);

                    // fixed fee Line
                    // var fixedFeeLine = new Stripe.StripeModels.OutputData();
                    // fixedFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(refundDate));
                    // fixedFeeLine.Account = $"Expences:FeeBay Fees:{feeBayName1}:Fixed Fee Per Sale";
                    // fixedFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                    // fixedFeeLine.Amount = fixedFeesRefunded;
                    // fixedFeeLine.TransactionId = orderId;
                    // fixedFeeLine.SortOrder = 3;
                    // outputData.Add(fixedFeeLine);

                    // variable fee line
                    // var variableFeeLine = new Stripe.StripeModels.OutputData();
                    // variableFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(refundDate));
                    // variableFeeLine.Account = $"Expenses:FeeBay Fees:{feeBayName1}:Final Value Fees";
                    // variableFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                    // variableFeeLine.Amount = variableFeesRefunded;
                    // variableFeeLine.TransactionId = orderId;
                    // variableFeeLine.SortOrder = 4;
                    // outputData.Add(variableFeeLine);

                    //international fees line
                    // if (internationalFeesRefunded != 0)
                    // {
                    //     var internationalFeeLine = new Stripe.StripeModels.OutputData();
                    //     internationalFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(refundDate));
                    //     internationalFeeLine.Account = $"Expenses:FeeBay Fees:{feeBayName1}:International Fee";
                    //     internationalFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                    //     internationalFeeLine.Amount = internationalFeesRefunded;
                    //     internationalFeeLine.TransactionId = orderId;
                    //     internationalFeeLine.SortOrder = 5;
                    //     outputData.Add(internationalFeeLine);
                    // }

                    // Define COGS as 1/2 of sale price
                    // And add to the cost of goods sold
                //     Stripe.StripeModels.OutputData feeBayCOGSRecord = new();
                //     feeBayCOGSRecord.Date = DateOnly.FromDateTime(DateTime.Parse(refundDate));
                //     feeBayCOGSRecord.Account = "Expenses:Cost of Goods Sold";
                //     feeBayCOGSRecord.Description = string.Empty;// $"feeBay Order #{orderId} - {refund.Item_title} REFUNDED COGS";
                //     if ((cogs == null) || (cogs == 0.0m))
                //     {
                //         feeBayCOGSRecord.Amount = -(sellingPriceRefunded / 2);
                //     }
                //     else
                //     {
                //         feeBayCOGSRecord.Amount = (decimal)-(cogs);
                //     }
                //     feeBayCOGSRecord.TransactionId = orderId;
                //     feeBayCOGSRecord.SortOrder = 6;
                //     outputData.Add(feeBayCOGSRecord);

                //     // now subtract the cost of the sold stuff from inventory
                //     Stripe.StripeModels.OutputData feeBayInventoryRecord = new();
                //     feeBayInventoryRecord.Date = DateOnly.FromDateTime(DateTime.Parse(refundDate));
                //     feeBayInventoryRecord.Account = "Assets:INVENTORY";
                //     feeBayInventoryRecord.Description = string.Empty;// incomeLineDescription;
                //     feeBayInventoryRecord.Amount = -(sellingPriceRefunded / 2);
                //     feeBayInventoryRecord.TransactionId = orderId;
                //     feeBayInventoryRecord.SortOrder = 7;
                //     outputData.Add(feeBayInventoryRecord);
                }
                catch (Exception)
                {
                    throw;
                }
           // throw new NotImplementedException();
        }

        private void HandleSale(Transaction transaction, Order associatedOrder)
        {
            if (transaction.OrderLineItems == null || !transaction.OrderLineItems.Any())
            {
                return;
            }
                var feeBaySellerID = "Simmons Ink";
                var outputData = new List<ToGnuCash>();
            
            foreach(var transactionLineItem in transaction.OrderLineItems)
            {
                var orderLineItem = associatedOrder.LineItems.Where(l => l.LineItemId == transactionLineItem.LineItemId).Single();
                var itemFees = transactionLineItem.MarketplaceFees;
                var skusInOrder = orderLineItem.SKU;
                var orderDate = associatedOrder.CreationDate;
                var orderId = transaction.OrderId;
                var transactionId = transaction.TransactionId;
                var sellingPrice = orderLineItem.LineItemCost.DollarAmount() ?? 0m;//order.Sum(x => decimal.Parse(x.Item_subtotal));
                var shippingPrice = orderLineItem.DeliveryCost.ShippingCost.DollarAmount();
                var fixedFees = itemFees.SingleOrDefault(f=>f.FeeType == FeeTypeEnum.FINAL_VALUE_FEE_FIXED_PER_ORDER)?.Amount?.DollarAmount() ?? 0m;
                var variableFees = itemFees.SingleOrDefault(f =>f.FeeType == FeeTypeEnum.FINAL_VALUE_FEE)?.Amount?.DollarAmount() ?? 0m;
                var internationalFees = itemFees.SingleOrDefault(f =>f.FeeType == FeeTypeEnum.INTERNATIONAL_FEE)?.Amount?.DollarAmount() ?? 0m;
                var gross = orderLineItem.Total.DollarAmount() ?? 0m;
                var net = gross - fixedFees - variableFees - internationalFees;
                var numberSold = orderLineItem.Quantity;
                var title = orderLineItem.Title;
                var incomeLineDescription = $"feeBay Order #{orderId} SKU: {skusInOrder} - {title} ";

                // income line
                var incomeLine = new ToGnuCash();
                incomeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));// 
                incomeLine.Account = $"Income:{feeBaySellerID} Sales";
                incomeLine.Description = incomeLineDescription;
                incomeLine.Amount = gross;
                incomeLine.TransactionId = transactionId;//orderId;
                incomeLine.SortOrder = 1;
                outputData.Add(incomeLine);

                // fixed fee line
                var fixedFeeLine = new ToGnuCash();
                fixedFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                fixedFeeLine.Account = $"Expences:FeeBay Fees:{feeBaySellerID}:Fixed Fee Per Sale";
                fixedFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                fixedFeeLine.Amount = fixedFees;
                fixedFeeLine.TransactionId = transactionId;//orderId;
                fixedFeeLine.SortOrder = 2;
                outputData.Add(fixedFeeLine);

                // variable fee line
                var variableFeeLine = new ToGnuCash();
                variableFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                variableFeeLine.Account = $"Expenses:FeeBay Fees:{feeBaySellerID}:Final Value Fees";
                variableFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                variableFeeLine.Amount = variableFees;
                variableFeeLine.TransactionId = transactionId;//orderId;
                variableFeeLine.SortOrder = 3;
                outputData.Add(variableFeeLine);

                //add the remaining money to feeBay current assett
                var netIncomeLine = new ToGnuCash();
                netIncomeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                netIncomeLine.Account = $"Assets:Current Assets:feeBay:{feeBaySellerID}";
                netIncomeLine.Description = string.Empty;
                netIncomeLine.Amount = -net;
                netIncomeLine.TransactionId = transactionId;//orderId;
                netIncomeLine.SortOrder = 4;
                outputData.Add(netIncomeLine);

                //international fees line
                if (internationalFees != 0)
                {
                    var internationalFeeLine = new ToGnuCash();
                    internationalFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                    internationalFeeLine.Account = $"Expenses:FeeBay Fees:{feeBaySellerID}:International Fee";
                    internationalFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                    internationalFeeLine.Amount = internationalFees;
                    internationalFeeLine.TransactionId = transactionId;//orderId;
                    internationalFeeLine.SortOrder = 5;
                    outputData.Add(internationalFeeLine);
                }

                // And add to the cost of goods sold
                ToGnuCash feeBayCOGSRecord = new();
                feeBayCOGSRecord.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                feeBayCOGSRecord.Account = "Expenses:Cost of Goods Sold";
                feeBayCOGSRecord.Description = $"{incomeLineDescription} COGS";
                feeBayCOGSRecord.Amount = MakeCogsForFullOrder(sellingPrice, skusInOrder);
                feeBayCOGSRecord.TransactionId = transactionId;//orderId;
                feeBayCOGSRecord.SortOrder = 6;
                outputData.Add(feeBayCOGSRecord);

                // now subtract the cost of the sold stuff from inventory
                ToGnuCash feeBayInventoryRecord = new();
                feeBayInventoryRecord.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                feeBayInventoryRecord.Account = "Assets:INVENTORY";
                feeBayInventoryRecord.Description = $"{incomeLineDescription} COGS";
                feeBayInventoryRecord.Amount = -MakeCogsForFullOrder(sellingPrice, skusInOrder);                    feeBayInventoryRecord.TransactionId = transactionId;//orderId;
                feeBayInventoryRecord.SortOrder = 7;
                outputData.Add(feeBayInventoryRecord);    
            } 
        }
        private decimal MakeCogsForFullOrder(decimal sellingPrice, string sku)
        {
            decimal fullOrderCogs = 0.0m;
            if (sellingPrice == 130.0m)
            {
                return fullOrderCogs;
            }

            var singleStampCogs = _localDbConnectionManager.GetStampCOGS(sku);
            if ((singleStampCogs == null) || (singleStampCogs == 0.0m))
            {
                singleStampCogs = -(sellingPrice / 2);
            }
            else
            {
                singleStampCogs = (decimal)-(singleStampCogs);
            }
            fullOrderCogs = (decimal)singleStampCogs;
            
            return fullOrderCogs;
        }








/*
        /*   foreach (var order in orderList)
 {


     var orderTransactions = transactionList
         .Where(t => string.Equals(t.OrderId, order.OrderId, StringComparison.OrdinalIgnoreCase))
         .ToList(); */

        /*   foreach (var lineItem in order.LineItems)
          {
              var lineTransactions = orderTransactions
                  .Where(t => t.OrderLineItems != null && t.OrderLineItems.Any(ol => string.Equals(ol.LineItemId, lineItem.LineItemId, StringComparison.OrdinalIgnoreCase)))
                  .ToList();

              var sourceTransactions = lineTransactions.Any() ? lineTransactions : orderTransactions;
              var primaryTransaction = sourceTransactions.FirstOrDefault();

              Payout? payout = null;
              if (primaryTransaction != null && string.IsNullOrWhiteSpace(primaryTransaction.PayoutId) == false)
              {
                  payoutsById.TryGetValue(primaryTransaction.PayoutId, out payout);
              }
*/
        /*   var grossAmount = FirstNonEmpty(
              lineItem.Total?.Value,
              primaryTransaction?.Amount?.Value,
              "0.00");

          var itemSubtotal = FirstNonEmpty(
              lineItem.LineItemCost?.Value,
              primaryTransaction?.TotalFeeBasisAmount?.Value,
              grossAmount,
              "0.00");

          var shippingAndHandling = FirstNonEmpty(
              lineItem.DeliveryCost?.ShippingCost?.Value,
              "0.00"); */

        /* var belowStandardFee = SumMarketplaceFee(sourceTransactions, lineItem.LineItemId, FeeTypeEnum.BELOW_STANDARD_FEE);
        var charityDonation = SumDonations(sourceTransactions, lineItem.LineItemId);
        var depositProcessingFee = SumMarketplaceFee(sourceTransactions, lineItem.LineItemId, FeeTypeEnum.DEPOSIT_PROCESSING_FEE);
        var finalValueFeeFixed = SumMarketplaceFee(sourceTransactions, lineItem.LineItemId, FeeTypeEnum.FINAL_VALUE_FEE_FIXED_PER_ORDER);
        var finalValueFeeVariable = SumMarketplaceFee(sourceTransactions, lineItem.LineItemId, FeeTypeEnum.FINAL_VALUE_FEE);
        var internationalFee = SumMarketplaceFee(sourceTransactions, lineItem.LineItemId, FeeTypeEnum.INTERNATIONAL_FEE);
        var inadFee = SumMarketplaceFee(sourceTransactions, lineItem.LineItemId, FeeTypeEnum.HIGH_ITEM_NOT_AS_DESCRIBED_FEE);
        var regulatoryFee = SumMarketplaceFee(sourceTransactions, lineItem.LineItemId, FeeTypeEnum.REGULATORY_OPERATING_FEE);

        var ebayCollectedTax = SumAmounts(sourceTransactions.Select(t => t.EBayCollectedTaxAmount));
        var sellerCollectedTax = SumAmounts(lineItem.Taxes?.Select(t => t.Amount));

        var netAmount = ParseAmount(grossAmount)
            - belowStandardFee
            - charityDonation
            - depositProcessingFee
            - finalValueFeeFixed
            - finalValueFeeVariable
            - internationalFee
            - inadFee
            - regulatoryFee;

     

        results.Add(row);
    } */


        // return results;


        private static decimal SumMarketplaceFee(IEnumerable<Transaction>? transactions, string? lineItemId, FeeTypeEnum feeType)
        {
            if (transactions == null)
            {
                return 0m;
            }

            decimal total = 0m;

            foreach (var transaction in transactions)
            {
                if (transaction?.OrderLineItems == null)
                {
                    continue;
                }

                foreach (var orderLine in transaction.OrderLineItems)
                {
                    if (orderLine == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(lineItemId) == false && string.Equals(orderLine.LineItemId, lineItemId, StringComparison.OrdinalIgnoreCase) == false)
                    {
                        continue;
                    }

                    if (orderLine.MarketplaceFees == null)
                    {
                        continue;
                    }

                    foreach (var fee in orderLine.MarketplaceFees)
                    {
                        if (fee?.FeeType == feeType)
                        {
                            total += ParseAmount(fee.Amount?.Value);
                        }
                    }
                }
            }

            return total;
        }

        private static decimal SumDonations(IEnumerable<Transaction>? transactions, string? lineItemId)
        {
            if (transactions == null)
            {
                return 0m;
            }

            decimal total = 0m;

            foreach (var transaction in transactions)
            {
                if (transaction?.OrderLineItems == null)
                {
                    continue;
                }

                foreach (var orderLine in transaction.OrderLineItems)
                {
                    if (orderLine == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(lineItemId) == false && string.Equals(orderLine.LineItemId, lineItemId, StringComparison.OrdinalIgnoreCase) == false)
                    {
                        continue;
                    }

                    if (orderLine.Donations == null)
                    {
                        continue;
                    }

                    foreach (var donation in orderLine.Donations)
                    {
                        total += ParseAmount(donation?.Amount?.Value);
                    }
                }
            }

            return total;
        }

        private static decimal SumAmounts(IEnumerable<Amount?>? amounts)
        {
            if (amounts == null)
            {
                return 0m;
            }

            decimal total = 0m;
            foreach (var amount in amounts)
            {
                total += ParseAmount(amount?.Value);
            }
            return total;
        }

        private static decimal ParseAmount(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0m;
            }

            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0m;
        }

        private static string ToMoney(decimal value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value) == false)
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }
        #endregion
        #endregion

        #region Methods
        #region Private Methods
        private async Task<List<Payout>> GetAllPayOutsPaginated(string filter, int limit = 50)
        {
            var allPayouts = new List<Payout>();
            int offset = 0;
            bool hasMore = true;

            while (hasMore)
            {
                // Append offset to filter if not the first page
                string paginatedFilter = offset > 0 ? $"{filter},offset:{offset}" : filter;

                PayoutList payoutsContainer = await _eBayController.GetPayouts(
                    filter, null, limit, offset);


                if (payoutsContainer.Payouts != null && payoutsContainer.Payouts.Any())
                {
                    allPayouts.AddRange(payoutsContainer.Payouts);
                    Console.WriteLine($"Retrieved {payoutsContainer.Payouts.Count} payouts (Total so far: {allPayouts.Count})");
                }

                // Check if there are more pages
                hasMore = !string.IsNullOrEmpty(payoutsContainer.Next);
                offset += limit;

                // Safety check: if we've retrieved all transactions
                if (allPayouts.Count >= payoutsContainer.Total)
                {
                    hasMore = false;
                }
            }

            Console.WriteLine($"Completed pagination. Total payouts retrieved: {allPayouts.Count}");
            return allPayouts;
        }


        private async Task<List<Transaction>> GetAllTransactionsPaginated(string filter, int limit = 50)
        {
            var allTransactions = new List<Transaction>();
            int offset = 0;
            bool hasMore = true;

            while (hasMore)
            {
                // Append offset to filter if not the first page
                string paginatedFilter = offset > 0 ? $"{filter},offset:{offset}" : filter;

                Transactions transactionsContainer = await _eBayController.GetTransactions(
                    filter,null,limit,offset);
                   

                if (transactionsContainer.TransactionList != null && transactionsContainer.TransactionList.Any())
                {
                    allTransactions.AddRange(transactionsContainer.TransactionList);
                    Console.WriteLine($"Retrieved {transactionsContainer.TransactionList.Count} transactions (Total so far: {allTransactions.Count})");
                }

                // Check if there are more pages
                hasMore = !string.IsNullOrEmpty(transactionsContainer.Next);
                offset += limit;

                // Safety check: if we've retrieved all transactions
                if (allTransactions.Count >= transactionsContainer.Total)
                {
                    hasMore = false;
                }
            }
            var look = from a in allTransactions where a.OrderId == "09-14052-99669" select a;
            Console.WriteLine($"Completed pagination. Total transactions retrieved: {allTransactions.Count}");
            return allTransactions;
        }

        private async Task<List<Order>> GetAllOrdersPaginated(string filter, int limit = 50)
        {
            var allOrders = new List<Order>();
            int offset = 0;
            bool hasMore = true;

            while (hasMore)
            {
                // Append offset to filter if not the first page
                string paginatedFilter = offset > 0 ? $"{filter},offset:{offset}" : filter;

                Orders ordersContainer = await _eBayController.GetOrders(
                    filter, limit, offset);

                if (ordersContainer.OrderList != null && ordersContainer.OrderList.Any())
                {
                    allOrders.AddRange(ordersContainer.OrderList);
                    Console.WriteLine($"Retrieved {ordersContainer.OrderList.Count} orders (Total so far: {allOrders.Count})");
                }

                // Check if there are more pages
                hasMore = !string.IsNullOrEmpty(ordersContainer.Next);
                offset += limit;

                // Safety check: if we've retrieved all orders
                if (allOrders.Count >= ordersContainer.Total)
                {
                    hasMore = false;
                }
            }

            Console.WriteLine($"Completed pagination. Total orders retrieved: {allOrders.Count}");
            return allOrders;
        }

        private async Task<bool> FormatToSendToGnuCash(List<Order> orders, List<Transaction> transactions, List<Payout> payouts)
        {
            try
            {
                // Instantiate converter with database connection manager
                var converter = new EbayToGnuCashConverter(_localDbConnectionManager);

                // Convert orders and transactions to GnuCash format
                Console.WriteLine($"Processing {orders.Count} orders, {transactions.Count} transactions, and {payouts.Count} payouts...");
                var gnuCashLines = converter.ConvertOrdersAndTransactions(orders, transactions, payouts, "Simmons_Ink");

                if (!gnuCashLines.Any())
                {
                    MessageBox.Show(
                        "No transactions were generated. This may indicate no PAYOUT transactions were found for the specified date range.",
                        "No Data to Export",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }

                // Create output path with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var outputPath = $@"D:\Exports\eBay_GnuCash_{timestamp}.csv";

                // Export to CSV
                CsvExporter.WriteToCsv(gnuCashLines, outputPath);

                // Show success message
                MessageBox.Show(
                    $"Successfully exported {gnuCashLines.Count} transaction lines to:\n\n{outputPath}\n\n" +
                    $"Orders processed: {orders.Count}\n" +
                    $"PAYOUT transactions found: {transactions.Count(t => t.TransactionStatus == TransactionStatusEnum.PAYOUT)}",
                    "Export Successful",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return true;
            }
            catch (Exception ex)
            {
                // Display user-friendly error message
                MessageBox.Show(
                    $"An error occurred while exporting to GnuCash:\n\n{ex.Message}\n\n" +
                    $"Please check the console output for detailed error information.",
                    "Export Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Console.WriteLine($"Error in FormatToSendToGnuCash: {ex}");
                return false;
            }
        }

        private async Task<SigningKey> GetOrCreateSigningKey(EbayController ebayController)
        {
            // 1. Try to get from database
            FeeBaySigningKeys? cachedKey = null;

            cachedKey = await _localDbConnectionManager.GetSigningKeyAsync();

            //// cachedKey = null; // Force create new key for testing
            if(cachedKey != null)
            {
                return cachedKey.ToSigningKey();
            }

            SigningKey key;

            {
                // 2. Create new if none exist
                key = await ebayController.CreateSigningKey();
            }

            // 3. Store in database
            await _localDbConnectionManager.SaveSigningKeyAsync(key.ToFeeBaySigningKey());

            return key;
        }
        public static string ToEbayDate(DateTime dateTime)
        {
            return dateTime.ToUniversalTime().ToString("o");
        }
        #endregion
        #endregion
    }
}
