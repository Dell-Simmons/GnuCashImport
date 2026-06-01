using EbaySharp.Controllers;
using EbaySharp.Entities.Common;
using EbaySharp.Entities.Develop.KeyManagement.SigningKey;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances.Transaction;
using EbaySharp.Entities.Develop.SellingApps.OrderManagement.Fulfillment.Order;
using FeeBayConnectionTester.Extensions;
using FeeBayOAuth.TokenService;
using LocalDBConnections;
using LocalDBConnections.StampDataDB.StampDataEntities;
using System;
using System.Collections.Generic;   
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
        
        private EbayController _eBayController;
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
            multiFilter = "transactionStatus:{PAYOUT},transactionDate:[2026-01-01T00:00:00.000Z..2026-01-31T23:59:59.000Z]";

            //TransactionSummary transactionPayoutSummary = 
            //    await ebayController.GetTransactionSummary(signingKey, multiFilter);
            ////!{ COMPLETED} is funds going from buyer to feeBay.
            //multiFilter = "transactionStatus:{COMPLETED},transactionDate:[2026-01-01T00:00:00.000Z..2026-01-31T23:59:59.000Z]";

            //TransactionSummary transactionCompletedSummary =
            //    await ebayController.GetTransactionSummary(signingKey, multiFilter);

            //!GetTransactions
            multiFilter = "transactionDate:[2025-12-01T00:00:00.000Z..2025-12-31T23:59:59.000Z]";
            Transactions financialTransactionsContainer = await _eBayController.GetTransactions(signingKey, multiFilter, sort: null, limit: 50);
            List<Transaction> financialTransactionList = financialTransactionsContainer.TransactionList;



            string ordersFilter = "creationdate:[2025-12-01T00:00:00.000Z..2025-12-31T23:59:59.999Z]";
            Orders ordersContainer = await _eBayController.GetOrders(ordersFilter,limit:50);
            List<Order> orderList = ordersContainer.OrderList;

            await FormatToSendToGnuCash(orderList,financialTransactionList);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }
        #endregion

        #region Methods
        #region Private Methods
        private async Task<bool> FormatToSendToGnuCash(List<Order> orders, List<Transaction> transactions)
        {
            foreach (var order in orders)
            {
                var orderId = order.OrderId;
                var financialTransactionForOrder = transactions.Where(x => x.OrderId == orderId);
                if (!financialTransactionForOrder.Any()) 
                { 

                }

                foreach (var orderItem in order.LineItems)
                {
                    ToGnuCash salesLineIncome = MakeSalesLine(order, financialTransactionForOrder, 1);
                    ToGnuCash shippingIncome = MakeShippingIncomeLine(order, financialTransactionForOrder, 2);
                    ToGnuCash fixedFeeLineExpense = MakeFixedFeeLine(order, financialTransactionForOrder, 3);
                    ToGnuCash variableFeeLineExpense = MakeVariableFeeLine(order, financialTransactionForOrder, 4);
                    ToGnuCash internationalFeeExpense = MakeInternationalFeeLine(order, financialTransactionForOrder, 5);
                    ToGnuCash feeBayAssetsLine = MakeFeeBayAssettsLine(order, financialTransactionForOrder, 6);
                    ToGnuCash cogsLine = MakeCOGSLine(order, financialTransactionForOrder, 7);
                    ToGnuCash inventoryLine = MakeInventoryLine(order, financialTransactionForOrder, 8);
                }
            }
            return true;
        }

        private ToGnuCash MakeInternationalFeeLine(Order order, IEnumerable<Transaction> financialTransactionForOrder, int v)
        {
            return new ToGnuCash();
            //international fees line
            //    try
            //    {
            //        if (internationalFees != 0)
            //        {
            //            var internationalFeeLine = new Stripe.StripeModels.OutputData();
            //            internationalFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
            //            internationalFeeLine.Account = $"Expenses:FeeBay Fees:{feeBayName1}:International Fee";
            //            internationalFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
            //            internationalFeeLine.Amount = internationalFees;
            //            internationalFeeLine.TransactionId = orderId;
            //            internationalFeeLine.SortOrder = 5;
            //            outputData.Add(internationalFeeLine);
            //        }
            //    }
            //    catch (Exception)
            //    {
            //        throw;
            //    }
        }
        private void Form1_Load_1(object sender, EventArgs e)
        {
        }

        private ToGnuCash MakeShippingIncomeLine(Order order, IEnumerable<Transaction> financialTransactionForOrder, int v) => throw new NotImplementedException();

        //! Sort into Transaction Types
        //var sales = transactions.Where(x => x.TransactionType == TransactionTypeEnum.SALE);


        //var totalTransactions = transactions.Count();
        //var salesTransactions = sales.Count();
        //var refundTransactions = refund.Count();
        //var creditTransactions = credit.Count();
        //var disputeTransactions = dispute.Count();
        //var shippingLabelTransactions = shippingLabel.Count();
        //var transferTransactions = transfer.Count();
        //var nonSaleChargeTransactions = nonSaleCharge.Count();
        //var adjustmentTransactions = adjustment.Count();
        //var withdrawalTransactions = withdrawal.Count();
        //var loanRepaymentTransactions = loanRepayment.Count();
        //var purchaseTransactions = purchase.Count();

        //! Sort into Transaction Status
        //var onHold = financialTransactionsContainer.Where(x => x.TransactionStatus == TransactionStatusEnum.FUNDS_ON_HOLD);
        //var processing = financialTransactionsContainer.Where(x => x.TransactionStatus == TransactionStatusEnum.FUNDS_PROCESSING);
        //var availableForPayout = financialTransactionsContainer.Where(x => x.TransactionStatus == TransactionStatusEnum.FUNDS_AVAILABLE_FOR_PAYOUT);
        //var payout = financialTransactionsContainer.Where(x => x.TransactionStatus == TransactionStatusEnum.PAYOUT);
        //var completed = financialTransactionsContainer.Where(x => x.TransactionStatus == TransactionStatusEnum.COMPLETED);
        //var failed = financialTransactionsContainer.Where(x => x.TransactionStatus == TransactionStatusEnum.FAILED);

        //var onHoldTransactions = onHold.Count();
        //var processingTransactions = processing.Count();
        //var availableForPayoutTransactions = availableForPayout.Count();
        //var payoutTransactions = payout.Count();
        //var completedTransactions = completed.Count();
        //var failedTransactions = failed.Count();



        //}
        //return true;
        //}

        private ToGnuCash MakeInventoryLine(Order order, IEnumerable<Transaction> financialTransactionForOrder, int sortOrder)
        {
            var line = new ToGnuCash();
            //    // now subtract the cost of the sold stuff from inventory
            //    try
            //    {
            //        Stripe.StripeModels.OutputData feeBayInventoryRecord = new();
            //        feeBayInventoryRecord.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
            //        feeBayInventoryRecord.Account = "Assets:INVENTORY";
            //        feeBayInventoryRecord.Description = $"{incomeLineDescription} COGS";
            //        feeBayInventoryRecord.Amount = -MakeCogsForFullOrder(sellingPrice, skusInOrder);
            //        feeBayInventoryRecord.TransactionId = orderId;
            //        feeBayInventoryRecord.SortOrder = 7;
            //        outputData.Add(feeBayInventoryRecord);
            //    }
            //    catch (Exception)
            //    {
            //        throw;
            //    }
            //}
            return line;
        }
        private ToGnuCash MakeCOGSLine(Order order, IEnumerable<Transaction> financialTransactionForOrder, int sortOrder)
        {
            var line = new ToGnuCash();
            // And add to the cost of goods sold
            //    try
            //    {
            //        Stripe.StripeModels.OutputData feeBayCOGSRecord = new();
            //        feeBayCOGSRecord.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
            //        feeBayCOGSRecord.Account = "Expenses:Cost of Goods Sold";
            //        feeBayCOGSRecord.Description = $"{incomeLineDescription} COGS";
            //        feeBayCOGSRecord.Amount = MakeCogsForFullOrder(sellingPrice, skusInOrder);
            //        feeBayCOGSRecord.TransactionId = orderId;
            //        feeBayCOGSRecord.SortOrder = 6;
            //        outputData.Add(feeBayCOGSRecord);
            //    }
            //    catch (Exception)
            //    {
            //        throw;
            //    }
            return line;
        }
        private ToGnuCash MakeFeeBayAssettsLine(Order order, IEnumerable<Transaction> financialTransactionForOrder, int sortOrder)
        {
            var line = new ToGnuCash();
            try
            {
                var net = financialTransactionForOrder.Sum(t => Decimal.Parse(t.Amount.Value));
                line.Date = DateTime.Parse(order.CreationDate);
                line.Account = $"Assets:Current Assets:feeBay:Simmons_Ink";
                line.Description = $"feeBay Order #{order.OrderId} - {order.LineItems.Count} items sold";
                line.Amount = -net;
                line.TransactionId = order.OrderId;
                line.SortOrder = sortOrder;
            }
            catch (Exception)
            {
                throw;
            }
            return line;
        }
        private ToGnuCash MakeVariableFeeLine(Order order, IEnumerable<Transaction> financialTransactionForOrder, int sortOrder)
        {
            var line = new ToGnuCash();
            //    // variable fee line
            //    try
            //    {
            //        var variableFeeLine = new Stripe.StripeModels.OutputData();
            //        variableFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
            //        variableFeeLine.Account = $"Expenses:FeeBay Fees:{feeBayName1}:Final Value Fees";
            //        variableFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
            //        variableFeeLine.Amount = variableFees;
            //        variableFeeLine.TransactionId = orderId;
            //        variableFeeLine.SortOrder = 3;
            //        outputData.Add(variableFeeLine);
            //    }
            //    catch (Exception)
            //    {
            //        throw;
            //    }
            return line;
        }
        private ToGnuCash MakeFixedFeeLine(Order order, IEnumerable<Transaction> financialTransactionForOrder, int sortOrder)
        {
            var line = new ToGnuCash();
            // fixed fee Line
            //    try
            //    {
            //        var fixedFeeLine = new Stripe.StripeModels.OutputData();
            //        fixedFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
            //        fixedFeeLine.Account = $"Expences:FeeBay Fees:{feeBayName1}:Fixed Fee Per Sale";
            //        fixedFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
            //        fixedFeeLine.Amount = fixedFees;
            //        fixedFeeLine.TransactionId = orderId;
            //        fixedFeeLine.SortOrder = 2;
            //        outputData.Add(fixedFeeLine);
            //    }
            //    catch (Exception)
            //    {
            //        throw;
            //    }
            return line;
        }
        private ToGnuCash MakeSalesLine(Order order, IEnumerable<Transaction> financialTransactionForOrder, int sortOrder)
        {
            // total sale less shipping charge
            var line = new ToGnuCash();
            line.Date = DateTime.Parse(order.CreationDate);
            line.Account = $"Income:feeBay SI Sales:Product Sale";
            line.Description = $"feeBay Order #{order.OrderId} - {order.LineItems.First().Title}";
            line.Amount = Decimal.Parse(order.PricingSummary.PriceSubtotal.Value);

            var SKU = order.LineItems.First().SKU;
            var productTitle = order.LineItems.First().Title;
            return line;
            //        var incomeLine = new Stripe.StripeModels.OutputData();
            //        incomeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
            //        incomeLine.Account = $"Income:{feeBayName2} Sales";
            //        incomeLine.Description = incomeLineDescription;
            //        incomeLine.Amount = sellingPrice + shippingPrice;
            //        incomeLine.TransactionId = orderId;
            //        incomeLine.SortOrder = 1;
            //        outputData.Add(incomeLine);
        }


        //    if (gross + fixedFees + variableFees + internationalFees != net)
        //    {
        //        MessageBox.Show(
        //            $"Net amount {net} does not equal gross amount {gross} - fixed fees {fixedFees} - variable fees {variableFees}");
        //    }
        //    var incomeLineDescription = string.Empty;
        //    if (numberSold == 1)
        //    {
        //        incomeLineDescription = $"feeBay Order #{orderId} - {order.First().Item_title}";
        //    }
        //    else
        //    {
        //        incomeLineDescription = $"feeBay Order #{orderId} - {numberSold} items sold";
        //    }

       

        private decimal ParseOutTotalShippingCharge(Transaction sale) 
        {
            var totalSale = Decimal.Parse(sale.Amount.Value);
            var totalSaleIncludingShipping = Decimal.Parse(sale.TotalFeeBasisAmount.Value);
            return totalSaleIncludingShipping - totalSale;
        }

        //private List<int> PullOutSkus(Transaction.OrderLineItems orderItems)
        //{
        //    List<int> skus = new();
        //    //foreach (var item in)
        //    //{
        //    //    if (item.Sku != null)
        //    //    {
        //    //        var parsedOk = int.TryParse(item.Sku, out int sku);
        //    //        if (parsedOk)
        //    //        {
        //    //            skus.Add(sku);
        //    //        }
        //    //    }
        //    //}
        //    return skus;
        //}
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
