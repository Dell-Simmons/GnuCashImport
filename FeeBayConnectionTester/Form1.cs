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
            multiFilter = "transactionDate:[2026-01-01T00:00:00.000Z..2026-01-31T23:59:59.000Z]";
            Transactions financialTransactionsContainer = await _eBayController.GetTransactions(signingKey, multiFilter);
            List<Transaction> financialTransactionList = financialTransactionsContainer.TransactionList;



            string ordersFilter = "creationdate:[2026-01-01T00:00:00.000Z..2026-01-31T23:59:59.999Z]\r\n";
            Orders ordersContainer = await _eBayController.GetOrders(ordersFilter);

            await FormatToSendToGnuCash(financialTransactionList);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }
        #endregion
        #region Methods
        #region Private Methods
        private async Task<bool> FormatToSendToGnuCash(List<Transaction> transactions)
        {
            //! Sort into Transaction Types
            var sales = transactions.Where(x => x.TransactionType == TransactionTypeEnum.SALE);
            var refund = transactions.Where(x => x.TransactionType == TransactionTypeEnum.REFUND);
            var credit = transactions.Where(x => x.TransactionType == TransactionTypeEnum.CREDIT);
            var dispute = transactions.Where(x => x.TransactionType == TransactionTypeEnum.DISPUTE);
            var shippingLabel = transactions.Where(x => x.TransactionType == TransactionTypeEnum.SHIPPING_LABEL);
            var transfer = transactions.Where(x => x.TransactionType == TransactionTypeEnum.TRANSFER);
            var nonSaleCharge = transactions.Where(x => x.TransactionType == TransactionTypeEnum.NON_SALE_CHARGE);
            var adjustment = transactions.Where(x => x.TransactionType == TransactionTypeEnum.ADJUSTMENT);
            var withdrawal = transactions.Where(x => x.TransactionType == TransactionTypeEnum.WITHDRAWAL);
            var loanRepayment = transactions.Where(x => x.TransactionType == TransactionTypeEnum.LOAN_REPAYMENT);
            var purchase = transactions.Where(x => x.TransactionType == TransactionTypeEnum.PURCHASE);

            var totalTransactions = transactions.Count();
            var salesTransactions = sales.Count();
            var refundTransactions = refund.Count();
            var creditTransactions = credit.Count();
            var disputeTransactions = dispute.Count();
            var shippingLabelTransactions = shippingLabel.Count();
            var transferTransactions = transfer.Count();
            var nonSaleChargeTransactions = nonSaleCharge.Count();
            var adjustmentTransactions = adjustment.Count();
            var withdrawalTransactions = withdrawal.Count();
            var loanRepaymentTransactions = loanRepayment.Count();
            var purchaseTransactions = purchase.Count();

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
            foreach (var sale in sales)
            {
              //  var orderId = sale.OrderId;
              ////  var orderInfo = await _eBayController.GetOrder(orderId);
              //  var orderDate = sale.TransactionDate;
              //  var sellingPrice = decimal.Parse(orderInfo.);
              //  var shippingPrice = ParseOutTotalShippingCharge(sale);


            }
            return true;
        }
            //foreach (var order in sales)
            //{
            //    var skusInOrder = PullOutSkus(order.OrderLineItems);
            //    var orderDate = order.First().Transaction_creation_date;
            //    var orderId = order.First().Order_number;
            //    var sellingPrice = order.Sum(x => decimal.Parse(x.Item_subtotal));
            //    var shippingPrice = order.Sum(x => decimal.Parse(x.Shipping_and_handling));
            //    var fixedFees = order.Sum(x => decimal.Parse(x.FVF_fixed));
            //    var variableFees = order.Sum(x => decimal.Parse(x.FVF_variable));
            //    var net = order.Sum(x => decimal.Parse(x.Net_amount));
            //    var internationalFees = order.Sum(x => decimal.Parse(x.International_fee));
            //    var gross = order.Sum(x => decimal.Parse(x.Gross_transaction_amount));
            //    var numberSold = order.Count(x => !string.Equals(x.Sku, "--", StringComparison.Ordinal));
            //    if (sellingPrice + shippingPrice != gross)
            //    {
            //        MessageBox.Show(
            //            $"Gross amount {gross} does not equal selling price {sellingPrice} + shipping price {shippingPrice}");
            //    }
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

            //    // income line
            //    try
            //    {
            //        var incomeLine = new Stripe.StripeModels.OutputData();
            //        incomeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
            //        incomeLine.Account = $"Income:{feeBayName2} Sales";
            //        incomeLine.Description = incomeLineDescription;
            //        incomeLine.Amount = sellingPrice + shippingPrice;
            //        incomeLine.TransactionId = orderId;
            //        incomeLine.SortOrder = 1;
            //        outputData.Add(incomeLine);
            //    }
            //    catch (Exception)
            //    {
            //        throw;
            //    }

            //    // fixed fee Line
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

            //    //add the remaining money to feeBay current assett
            //    try
            //    {
            //        var netIncomeLine = new Stripe.StripeModels.OutputData();
            //        netIncomeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
            //        netIncomeLine.Account = $"Assets:Current Assets:feeBay:{feeBayName2}";
            //        netIncomeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
            //        netIncomeLine.Amount = -net;
            //        netIncomeLine.TransactionId = orderId;
            //        netIncomeLine.SortOrder = 4;
            //        outputData.Add(netIncomeLine);
            //    }
            //    catch (Exception)
            //    {
            //        throw;
            //    }

            //    //international fees line
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

            //    // And add to the cost of goods sold
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


        //}

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
