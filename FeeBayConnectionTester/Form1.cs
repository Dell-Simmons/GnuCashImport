using EbaySharp.Controllers;
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
using System;
using System.Globalization;
using System.Linq;

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
        #region
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
            if(string.IsNullOrWhiteSpace(token))
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
            string payOutsFilter = "payoutDate:[2026-01-01T00:00:00.000Z..2026-06-14T23:59:59.999Z]";
            List<Payout> payOutList = await GetAllPayOutsPaginated(payOutsFilter, limit: 50);

            //!GetTransactions with pagination
            multiFilter = "transactionDate:[2025-12-25T00:00:00.000Z..2026-06-14T23:59:59.000Z]";
            List<Transaction> transactionList = await GetAllTransactionsPaginated(multiFilter, limit: 50);

            //!GetOrders with pagination
            string ordersFilter = "creationdate:[2025-12-25T00:00:00.000Z..2026-06-14T23:59:59.999Z]";
            List<Order> orderList = await GetAllOrdersPaginated(ordersFilter, limit: 50);

            List<ToGnuCash> feeBayIncomingData = await CombineDownloadedData(
                payOutList,
                transactionList,
                orderList);
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
        #endregion

        #region
        private void Form1_Load(object sender, EventArgs e)
        {
        }
        #endregion
        #endregion
        #region Methods
        #region Public Methods
        public static string ToEbayDate(DateTime dateTime) => dateTime.ToUniversalTime().ToString("o");
        #endregion

        #region Private Methods
        #region Download Financials
        private async Task<List<Order>> GetAllOrdersPaginated(string filter, int limit = 50)
        {
            var allOrders = new List<Order>();
            int offset = 0;
            bool hasMore = true;

            while (hasMore)
            {
                // Append offset to filter if not the first page
                string paginatedFilter = offset > 0 ? $"{filter},offset:{offset}" : filter;

                Orders ordersContainer = await _eBayController.GetOrders(filter, limit, offset);

                if (ordersContainer.OrderList != null && ordersContainer.OrderList.Any())
                {
                    allOrders.AddRange(ordersContainer.OrderList);
                    Console.WriteLine(
                        $"Retrieved {ordersContainer.OrderList.Count} orders (Total so far: {allOrders.Count})");
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

        private async Task<List<Payout>> GetAllPayOutsPaginated(string filter, int limit = 50)
        {
            var allPayouts = new List<Payout>();
            int offset = 0;
            bool hasMore = true;

            while (hasMore)
            {
                // Append offset to filter if not the first page
                string paginatedFilter = offset > 0 ? $"{filter},offset:{offset}" : filter;

                PayoutList payoutsContainer = await _eBayController.GetPayouts(filter, null, limit, offset);


                if (payoutsContainer.Payouts != null && payoutsContainer.Payouts.Any())
                {
                    allPayouts.AddRange(payoutsContainer.Payouts);
                    Console.WriteLine(
                        $"Retrieved {payoutsContainer.Payouts.Count} payouts (Total so far: {allPayouts.Count})");
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

                Transactions transactionsContainer = await _eBayController.GetTransactions(filter, null, limit, offset);


                if (transactionsContainer.TransactionList != null && transactionsContainer.TransactionList.Any())
                {
                    allTransactions.AddRange(transactionsContainer.TransactionList);
                    Console.WriteLine(
                        $"Retrieved {transactionsContainer.TransactionList.Count} transactions (Total so far: {allTransactions.Count})");
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

        #endregion
        #region TransactionTypes
        private async Task<List<ToGnuCash>> CombineDownloadedData(
            List<Payout> payOutList,
            List<Transaction> transactionList,
            List<Order> orderList)
        {
            var results = new List<ToGnuCash>();
            //! just as a check look for transactions not associated with any payout
            var transactionsWithoutPayout = transactionList
                .Where(t => string.IsNullOrWhiteSpace(t.PayoutId))
                .ToList();
            if(transactionsWithoutPayout.Any())
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
            foreach(var payout in payOutList)
            {
                var payoutId = payout.PayoutId;
                int? numTransactionsInPayout = payout.TransactionCount;
                List<Transaction>? transactionsInThisPayout;
               
                groupedTransactions.TryGetValue(payoutId, out transactionsInThisPayout);
                if(transactionsInThisPayout == null)
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
                    //! HERE HERE HERE 
                    //! as a test of refund handling only generate ToGnuCash for one order
                    //! that got a full retund (actually lost in the mail)
                    //! HERE HERE HERE
                    var transactionId = transaction.TransactionId;
                    if(transaction.OrderId != "04-14077-74357")
                    {
                       continue;
                    }
                    // if(transaction.TransactionType == TransactionTypeEnum.REFUND)
                    // {
                    //     continue;
                    // }
                    //! HERE HERE HERE
                    //! above here
                    var associatedOrder = orderList.Where(o => o.OrderId == transaction.OrderId).FirstOrDefault();
                    if(associatedOrder == null)
                    {
                        // continue;
                    }
                    switch(transaction.TransactionType)
                    {
                        case TransactionTypeEnum.SALE:
                            List<ToGnuCash> saleLines = HandleSale(transaction, associatedOrder);
                            results.AddRange(saleLines);
                            break;

                        case TransactionTypeEnum.REFUND:
                            if(associatedOrder?.OrderPaymentStatus == OrderPaymentStatusEnum.FULLY_REFUNDED)
                            {
                              List<ToGnuCash> refundLines =  HandleFullRefund(transaction, associatedOrder);
                              results.AddRange(refundLines);
                                break;
                            }
                            if(associatedOrder?.OrderPaymentStatus == OrderPaymentStatusEnum.PARTIALLY_REFUNDED)
                            {
                               List<ToGnuCash> refundLines = HandlePartialRefund(transaction, associatedOrder   );
                               results.AddRange(refundLines);
                                break;
                            }
                            throw new NotSupportedException(
                                $"Unsupported order payment status in REFUND: {associatedOrder.OrderPaymentStatus}");

                        case TransactionTypeEnum.CREDIT:
                          List<ToGnuCash> creditLines =  HandleCredit(associatedOrder);
                           results.AddRange(creditLines);
                            break;

                        case TransactionTypeEnum.DISPUTE:
                              List<ToGnuCash> disputeLines = HandleDispute(associatedOrder);
                            results.AddRange(disputeLines);
                            break;

                        case TransactionTypeEnum.SHIPPING_LABEL:
                             List<ToGnuCash> shippingLabelLines =  HandleShipping_Label(transaction);
                           results.AddRange(shippingLabelLines);
                            break;

                        case TransactionTypeEnum.TRANSFER:
                            List<ToGnuCash> transferLines =   HanedleTransfer(associatedOrder);
                            results.AddRange(transferLines);
                            break;

                        case TransactionTypeEnum.NON_SALE_CHARGE:
                             List<ToGnuCash> nonSaleChargeLines =  HandleNon_Sale_Charge(transaction);
                            results.AddRange(nonSaleChargeLines);
                            break;

                        case TransactionTypeEnum.ADJUSTMENT:
                              List<ToGnuCash> adjustmentLines = HandleAdjustment(associatedOrder);
                            results.AddRange(adjustmentLines);
                            break;

                        case TransactionTypeEnum.WITHDRAWAL:
                              List<ToGnuCash> withdrawalLines = HandleWithdrawal(associatedOrder);
                            results.AddRange(withdrawalLines);
                            break;

                        case TransactionTypeEnum.LOAN_REPAYMENT:
                              List<ToGnuCash> loanRepaymentLines = HandleLoan_Repayment(associatedOrder);
                            results.AddRange(loanRepaymentLines);
                            break;

                        case TransactionTypeEnum.PURCHASE:
                              List<ToGnuCash> purchaseLines = HandlePurchase(associatedOrder);
                           results.AddRange(purchaseLines);
                            break;

                        case null:
                        default:
                            continue;
                    }
                }
            }
            return results;
        }

        private List<ToGnuCash> HandleAdjustment(Order? associatedOrder)
        {
            if (associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }
            return new List<ToGnuCash>();
        }

        private List<ToGnuCash> HandleCredit(Order? associatedOrder)
        {
            if (associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }
            return new List<ToGnuCash>();
        }

        private List<ToGnuCash> HandleDispute(Order? associatedOrder)
        {
            if (associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }
            return new List<ToGnuCash>();
        }

        private List<ToGnuCash> HandleFullRefund(Transaction transaction, Order? associatedOrder)
        {
            //! THIS WORKS THIS WORKS THIS WORKS DON'T CHANGE IT
            //! THIS WORKS THIS WORKS THIS WORKS DON'T CHANGE IT
            //! DO NOT MODIFY ANY OF THE LOGIC BELOW THIS LINE WITHOUT THOROUGH TESTING

            if (associatedOrder == null)
            {
                // associated order is null, cannot handle refund
                throw new ArgumentNullException(nameof(associatedOrder));
            }

            if (associatedOrder.LineItems.First().Title.Contains("RW8"))
            {
            }
            // this works leave it alone
            var feeBaySellerID = "Simmons Ink";
            var outputData = new List<ToGnuCash>();

            //
            foreach (var transactionLineItem in transaction.OrderLineItems)
            {
                var orderLineItem = associatedOrder.LineItems
                    .Where(l => l.LineItemId == transactionLineItem.LineItemId)
                    .Single();

                //! ASSUME FOR NOW THAT A FULL REFUND INDICATES THE STAMP WAS RETURNED
                //! not true in all cases (e.g. lost in the mail ), but assume for now


                var cogs = _localDbConnectionManager.GetStampCOGS(orderLineItem.SKU);
                var refundDate = DateOnly.FromDateTime(DateTime.Parse(transaction.TransactionDate));
                var orderId = transaction.OrderId;
                var sellingPriceRefunded = orderLineItem.LineItemCost.DollarAmount() ?? 0m;
                var shippingPriceRefunded = orderLineItem.DeliveryCost.ShippingCost.DollarAmount() ?? 0m;
                var totalRefund = SumUpRefunds(orderLineItem.Refunds);
                var fixedFeesRefunded = FindFixedFeesRefunded(transactionLineItem.MarketplaceFees);            // var variableFeesRefunded = Decimal.Parse(refund.FVF_variable);
                var variableFeesRefunded = FindVariableFeesRefunded(transactionLineItem.MarketplaceFees);
                var internationalFeesRefunded = FindInternationalFeesRefunded(transactionLineItem.MarketplaceFees);
                  var skusInOrder = orderLineItem.SKU;
                 var numberSold = orderLineItem.Quantity;
                var title = orderLineItem.Title;
                var incomeLineDescription = $"feeBay Order #{orderId} SKU: {skusInOrder} - {title} - REFUND";
                var netRefund = totalRefund - fixedFeesRefunded - variableFeesRefunded - internationalFeesRefunded;
            
                // var netRefund = Decimal.Parse(refund.Net_amount);
                // var internationalFeesRefunded = Decimal.Parse(refund.International_fee);
                // var grossRefund = Decimal.Parse(refund.Gross_transaction_amount);
                //! income line - product
                var incomeLine = new ToGnuCash();
                incomeLine.Date = refundDate; // 
                incomeLine.Account = $"Income:{feeBaySellerID} Sales";
                incomeLine.Description = incomeLineDescription;
                // set the income line amount to the negative of the selling price   
                incomeLine.Amount = -sellingPriceRefunded;
                incomeLine.TransactionId = transaction.TransactionId;//orderId;
                incomeLine.SortOrder = 1;
                outputData.Add(incomeLine);

                //! income line - shipping
                var shippingIncomeLine = new ToGnuCash();
                shippingIncomeLine.Date = refundDate;
                shippingIncomeLine.Account = $"Income:{feeBaySellerID} Shipping";
                shippingIncomeLine.Description = string.Empty;//$"feeBay Order #{orderId} SKU: {skusInOrder} - {title} ";//";
               // set the shipping line amount to the negative of the shipping price
                shippingIncomeLine.Amount = -shippingPriceRefunded;
                shippingIncomeLine.TransactionId = transaction.TransactionId;//orderId;
                shippingIncomeLine.SortOrder = 2;
                outputData.Add(shippingIncomeLine);

                //! fixed fee line
                var fixedFeeLine = new ToGnuCash();
                fixedFeeLine.Date = refundDate;
                fixedFeeLine.Account = $"Expences:FeeBay Fees:{feeBaySellerID}:Fixed Fee Per Sale";
                fixedFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
               // feeBay refunds the fees back to you so this is positive
                fixedFeeLine.Amount = fixedFeesRefunded;
                fixedFeeLine.TransactionId = transaction.TransactionId;//orderId;
                fixedFeeLine.SortOrder = 3;
                outputData.Add(fixedFeeLine);

                //! variable fee line
                var variableFeeLine = new ToGnuCash();
                variableFeeLine.Date = refundDate;
                variableFeeLine.Account = $"Expenses:FeeBay Fees:{feeBaySellerID}:Final Value Fees";
                variableFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                // feeBay refunds the fees back to you so this is positive
                variableFeeLine.Amount = variableFeesRefunded;
                variableFeeLine.TransactionId = transaction.TransactionId;//orderId;
                variableFeeLine.SortOrder = 4;
                outputData.Add(variableFeeLine);

                //!remove the remaining money from feeBay current assett
                var netIncomeLine = new ToGnuCash();
                netIncomeLine.Date = refundDate;
                netIncomeLine.Account = $"Assets:Current Assets:feeBay:{feeBaySellerID}";
                netIncomeLine.Description = string.Empty;
                netIncomeLine.Amount =  netRefund;
                netIncomeLine.TransactionId = transaction.TransactionId;//orderId;
                netIncomeLine.SortOrder = 5;
                outputData.Add(netIncomeLine);

                //!international fees line
                if(internationalFeesRefunded != 0)
                {
                    var internationalFeeLine = new ToGnuCash();
                    internationalFeeLine.Date = refundDate;
                    internationalFeeLine.Account = $"Expenses:FeeBay Fees:{feeBaySellerID}:International Fee";
                    internationalFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                    internationalFeeLine.Amount = internationalFeesRefunded;
                    internationalFeeLine.TransactionId = transaction.TransactionId;//orderId;
                    internationalFeeLine.SortOrder = 6;
                    outputData.Add(internationalFeeLine);
                }

                //! cost of goods sold goes down as items are refunded
                ToGnuCash feeBayCOGSRecord = new();
                feeBayCOGSRecord.Date = refundDate;
                feeBayCOGSRecord.Account = "Expenses:Cost of Goods Sold";
                feeBayCOGSRecord.Description = string.Empty;//$"{incomeLineDescription} COGS";
                feeBayCOGSRecord.Amount = -MakeCogsForFullOrder(sellingPriceRefunded, skusInOrder);
                feeBayCOGSRecord.TransactionId = transaction.TransactionId;//orderId;
                feeBayCOGSRecord.SortOrder = 7;
                outputData.Add(feeBayCOGSRecord);

                //! inventory value goes up as stamp is added back in . . . LOL
                ToGnuCash feeBayInventoryRecord = new();
                feeBayInventoryRecord.Date = refundDate;
                feeBayInventoryRecord.Account = "Assets:INVENTORY";
                feeBayInventoryRecord.Description = string.Empty;//$"{incomeLineDescription} COGS";
                feeBayInventoryRecord.Amount = MakeCogsForFullOrder(sellingPriceRefunded, skusInOrder);
                feeBayInventoryRecord.TransactionId = transaction.TransactionId;//orderId;
                feeBayInventoryRecord.SortOrder = 8;
                outputData.Add(feeBayInventoryRecord);
            }
            return outputData;
        }

        private List<ToGnuCash> HandleLoan_Repayment(Order? associatedOrder)
        {
            if(associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }
            return new List<ToGnuCash>();
        }
        private List<ToGnuCash> HandleNon_Sale_Charge(Transaction transaction)
        {
            //! figure out what type of fee this is
            var feeTypes = transaction.References;
            if(feeTypes == null)
            {
                //!must be a store subscription charge
              return  HandleStoreSubscriptionCharge();
            }
            foreach(var fee in feeTypes)
            {
                switch(fee.ReferenceType,transaction.TransactionMemo)
                {
                    case (ReferenceTypeEnum.ITEM_ID, "Promoted Listings - Priority fee"):
                       return HandlePromotedListingsPriorityFees(transaction, fee.ReferenceId);
                    default:
                        throw new NotImplementedException();
                }
            }
            return new List<ToGnuCash>();
        }
        private List<ToGnuCash> HandlePartialRefund(Transaction transaction, Order? associatedOrder)
        {
                if (associatedOrder == null)
            {
                // associated order is null, cannot handle refund
                throw new ArgumentNullException(nameof(associatedOrder));
            }

            // this works leave it alone
            var feeBaySellerID = "Simmons Ink";
            var outputData = new List<ToGnuCash>();
            var refundDate = DateOnly.FromDateTime(DateTime.Parse(transaction.TransactionDate));
           var orderId = associatedOrder.OrderId;
            //! determine if the partial refund is less than the total shipping charges
            //! if it is then treat the refund as a shipping refund rather than a product refund
            var totalShippingCharges = associatedOrder.LineItems.Sum(li => li.DeliveryCost.ShippingCost.DollarAmount() ?? 0m);
            var partialRefundAmount = (transaction.Amount.DollarAmount() ?? 0m) + (transaction.TotalFeeAmount.DollarAmount() ?? 0m);
            var feeBasisAmount = transaction.TotalFeeBasisAmount.DollarAmount() ?? 0m;
             //  var internationalFeesRefunded = FindInternationalFeesRefunded(transactionLineItem.MarketplaceFees);
             

            if(partialRefundAmount != feeBasisAmount)
            {
                // nope 
            }
            if(transaction.Amount.DollarAmount() < totalShippingCharges)
            {
                //! treat the refund as a shipping refund
                //! THIS WORKS
                outputData = HandlePartialShippingRefund(transaction, refundDate, partialRefundAmount, feeBaySellerID, orderId);
            }
            else
            {
                //! treat the refund as a product refund
                //! THIS WORKS
                outputData = HandlePartialProductRefund(transaction, associatedOrder);
            }
            return outputData;   
        }

        private List<ToGnuCash> HandlePartialProductRefund(Transaction transaction, Order associatedOrder)
        {
            var feeBaySellerID = "Simmons Ink";
            var outputData = new List<ToGnuCash>();
            var refundDate = DateOnly.FromDateTime(DateTime.Parse(transaction.TransactionDate));
            var orderId = associatedOrder.OrderId;
            //! determine if the partial refund is less than the total shipping charges
            //! if it is then treat the refund as a shipping refund rather than a product refund
            var totalShippingCharges = associatedOrder.PricingSummary.DeliveryCost.DollarAmount() ?? 0m;        //.LineItems.Sum(li => li.DeliveryCost.ShippingCost.DollarAmount() ?? 0m);
            var totalLineItemPrices = associatedOrder.PricingSummary.PriceSubtotal.DollarAmount() ?? 0m;
            var totalFees = associatedOrder.TotalMarketplaceFee.DollarAmount() ?? 0m;
            
            var partialRefundAmount = (transaction.Amount.DollarAmount() ?? 0m) + (transaction.TotalFeeAmount.DollarAmount() ?? 0m);
            //var summedItemRefunds = 0m; 
            var summedItemRefunds = associatedOrder.LineItems.Sum(li => li.Refunds.Sum(r => r.Amount.DollarAmount() ?? 0m));    
             if(summedItemRefunds != partialRefundAmount )
                {
                    // handle the case where the summed item refunds do not match the partial refund amount
                    // this could indicate an inconsistency that needs to be addressed
                    throw new InvalidOperationException("Summed item refunds do not match the partial refund amount.");
                }
        foreach (var transactionLineItem in transaction.OrderLineItems)
            {
                var orderLineItem = associatedOrder.LineItems
                    .Where(l => l.LineItemId == transactionLineItem.LineItemId)
                    .Single();

            var skusInOrder = orderLineItem.SKU;
            var numberSold = orderLineItem.Quantity;
            var title = orderLineItem.Title;
            var incomeLineDescription = $"feeBay Order #{orderId} SKU: {skusInOrder} - {title} - REFUND";
             
            var lineItemRefund = SumUpRefunds(orderLineItem.Refunds);
            var fixedFeesRefunded = FindFixedFeesRefunded(transactionLineItem.MarketplaceFees);            // var variableFeesRefunded = Decimal.Parse(refund.FVF_variable);
            var variableFeesRefunded = FindVariableFeesRefunded(transactionLineItem.MarketplaceFees);
            var internationalFeesRefunded = FindInternationalFeesRefunded(transactionLineItem.MarketplaceFees);
               

            var lineItemPrice = orderLineItem.LineItemCost.DollarAmount() ?? 0m;
            //var lineItemRefund = orderLineItem.Refunds.Sum(r => r.Amount.DollarAmount() ?? 0m);
               
             if(partialRefundAmount == lineItemPrice)
            {
                   // since the partial refund amount matches the price of this order line item, 
                   // assume that this itemis the one being refunded.  
                   // it can be treated as a full refund for this specific item  
            }
            else
            {
                // since the partial refund amount does not match the price of this order line item,
                // prorate the partial refund amount across all line items in the order
               // var totalLineItemPrices = associatedOrder.LineItems.Sum(li => li.LineItemCost.DollarAmount() ?? 0m);
               //! income line - product
                var incomeLine = new ToGnuCash();
                incomeLine.Date = refundDate; // 
                incomeLine.Account = $"Income:{feeBaySellerID} Sales";
                incomeLine.Description = incomeLineDescription;
                // set the income line amount to the negative of the selling price   
                incomeLine.Amount = -lineItemRefund;
                incomeLine.TransactionId = transaction.TransactionId;//orderId;
                incomeLine.SortOrder = 1;
                outputData.Add(incomeLine);

                //! income line - shipping
                // no change
                //! fixed fee line
                // no change
                  //! variable fee line
                var variableFeeLine = new ToGnuCash();
                variableFeeLine.Date = refundDate;
                variableFeeLine.Account = $"Expenses:FeeBay Fees:{feeBaySellerID}:Final Value Fees";
                variableFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                // feeBay refunds the fees back to you so this is positive
               // variableFeeLine.Amount = variableFeesRefunded;
                variableFeeLine.TransactionId = transaction.TransactionId;//orderId;
                variableFeeLine.SortOrder = 4;
                outputData.Add(variableFeeLine);

                 
                 
                 
                 
                  // cogs does not change b/c item is not returned, just has a reduced selling price
               
               
               
               
               
                
               



            }
         }
            return outputData;   
        }

        private List<ToGnuCash> HandlePurchase(Order? associatedOrder)
        {
            if(associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }

           return new List<ToGnuCash>();
        }
        private List<ToGnuCash> HandleSale(Transaction transaction, Order? associatedOrder)
        {
            //! THIS WORKS THIS WORKS THIS WORKS DON'T CHANGE IT
            //! THIS WORKS THIS WORKS THIS WORKS DON'T CHANGE IT
            //! DO NOT MODIFY ANY OF THE LOGIC BELOW THIS LINE WITHOUT THOROUGH TESTING

            if(associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }
            if(transaction.OrderLineItems == null || !transaction.OrderLineItems.Any())
            {
                return new List<ToGnuCash>();
            }
            var feeBaySellerID = "Simmons Ink";
            var outputData = new List<ToGnuCash>();

            foreach(var transactionLineItem in transaction.OrderLineItems)
            {
                var orderLineItem = associatedOrder.LineItems
                    .Where(l => l.LineItemId == transactionLineItem.LineItemId)
                    .Single();
                var itemFees = transactionLineItem.MarketplaceFees;
                var skusInOrder = orderLineItem.SKU;
                var orderDate = associatedOrder.CreationDate;
                var orderId = transaction.OrderId;
                var transactionId = transaction.TransactionId;
                var sellingPrice = orderLineItem.LineItemCost.DollarAmount() ?? 0m;//order.Sum(x => decimal.Parse(x.Item_subtotal));
                var shippingPrice = orderLineItem.DeliveryCost.ShippingCost.DollarAmount() ?? 0m;
                var fixedFees = itemFees.SingleOrDefault(f => f.FeeType == FeeTypeEnum.FINAL_VALUE_FEE_FIXED_PER_ORDER)?.Amount?.DollarAmount(
                        ) ?? 0m;
                var variableFees = itemFees.SingleOrDefault(f => f.FeeType == FeeTypeEnum.FINAL_VALUE_FEE)?.Amount?.DollarAmount(
                        ) ?? 0m;
                var internationalFees = itemFees.SingleOrDefault(f => f.FeeType == FeeTypeEnum.INTERNATIONAL_FEE)?.Amount?.DollarAmount(
                        ) ?? 0m;
                var gross = orderLineItem.Total.DollarAmount() ?? 0m;
                var net = gross - fixedFees - variableFees - internationalFees;
                var numberSold = orderLineItem.Quantity;
                var title = orderLineItem.Title;
                var incomeLineDescription = $"feeBay Order #{orderId} SKU: {skusInOrder} - {title} ";

                //! income line - product
                var incomeLine = new ToGnuCash();
                incomeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));// 
                incomeLine.Account = $"Income:{feeBaySellerID} Sales";
                incomeLine.Description = incomeLineDescription;
                incomeLine.Amount = sellingPrice;
                incomeLine.TransactionId = transactionId;//orderId;
                incomeLine.SortOrder = 1;
                outputData.Add(incomeLine);

                //! income line - shipping
                var shippingIncomeLine = new ToGnuCash();
                shippingIncomeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                shippingIncomeLine.Account = $"Income:{feeBaySellerID} Shipping";
                shippingIncomeLine.Description = string.Empty;//$"feeBay Order #{orderId} SKU: {skusInOrder} - {title} ";//";
                shippingIncomeLine.Amount = shippingPrice;
                shippingIncomeLine.TransactionId = transactionId;//orderId;
                shippingIncomeLine.SortOrder = 2;
                outputData.Add(shippingIncomeLine);

                //! fixed fee line
                var fixedFeeLine = new ToGnuCash();
                fixedFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                fixedFeeLine.Account = $"Expences:FeeBay Fees:{feeBaySellerID}:Fixed Fee Per Sale";
                fixedFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                fixedFeeLine.Amount = -fixedFees;
                fixedFeeLine.TransactionId = transactionId;//orderId;
                fixedFeeLine.SortOrder = 3;
                outputData.Add(fixedFeeLine);

                //! variable fee line
                var variableFeeLine = new ToGnuCash();
                variableFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                variableFeeLine.Account = $"Expenses:FeeBay Fees:{feeBaySellerID}:Final Value Fees";
                variableFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                variableFeeLine.Amount = -variableFees;
                variableFeeLine.TransactionId = transactionId;//orderId;
                variableFeeLine.SortOrder = 4;
                outputData.Add(variableFeeLine);

                //add the remaining money to feeBay current assett
                var netIncomeLine = new ToGnuCash();
                netIncomeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                netIncomeLine.Account = $"Assets:Current Assets:feeBay:{feeBaySellerID}";
                netIncomeLine.Description = string.Empty;
                netIncomeLine.Amount = -net;
                netIncomeLine.TransactionId = transactionId;//orderId;
                netIncomeLine.SortOrder = 5;
                outputData.Add(netIncomeLine);

                //!international fees line
                if(internationalFees != 0)
                {
                    var internationalFeeLine = new ToGnuCash();
                    internationalFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                    internationalFeeLine.Account = $"Expenses:FeeBay Fees:{feeBaySellerID}:International Fee";
                    internationalFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                    internationalFeeLine.Amount = -internationalFees;
                    internationalFeeLine.TransactionId = transactionId;//orderId;
                    internationalFeeLine.SortOrder = 6;
                    outputData.Add(internationalFeeLine);
                }

                // And add to the cost of goods sold
                ToGnuCash feeBayCOGSRecord = new();
                feeBayCOGSRecord.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                feeBayCOGSRecord.Account = "Expenses:Cost of Goods Sold";
                feeBayCOGSRecord.Description = string.Empty;//$"{incomeLineDescription} COGS";
                feeBayCOGSRecord.Amount = MakeCogsForFullOrder(sellingPrice, skusInOrder);
                feeBayCOGSRecord.TransactionId = transactionId;//orderId;
                feeBayCOGSRecord.SortOrder = 7;
                outputData.Add(feeBayCOGSRecord);

                // now subtract the cost of the sold stuff from inventory
                ToGnuCash feeBayInventoryRecord = new();
                feeBayInventoryRecord.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                feeBayInventoryRecord.Account = "Assets:INVENTORY";
                feeBayInventoryRecord.Description = string.Empty;//$"{incomeLineDescription} COGS";
                feeBayInventoryRecord.Amount = -MakeCogsForFullOrder(sellingPrice, skusInOrder);
                feeBayInventoryRecord.TransactionId = transactionId;//orderId;
                feeBayInventoryRecord.SortOrder = 8;
                outputData.Add(feeBayInventoryRecord);
            }
            return outputData;
        }
        private List<ToGnuCash> HandleShipping_Label(Transaction label)//, Order? associatedOrder)
        {
         
            var feeBaySellerID = "Simmons Ink";
            var outputData = new List<ToGnuCash>();
            var labelLineFrom = new ToGnuCash();
            labelLineFrom.Date = DateOnly.FromDateTime(DateTime.Parse(label.TransactionDate));
            labelLineFrom.Account = $"Assets:Current Assets:feeBay:{feeBaySellerID}";
            labelLineFrom.Description = $"{label.OrderId} - {label.TransactionMemo}";
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
           
            return outputData;
        }
        private List<ToGnuCash> HanedleTransfer(Order? associatedOrder)
        {
            if(associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }
            return new List<ToGnuCash>();
        }
        private List<ToGnuCash> HandleWithdrawal(Order? associatedOrder)
        {
            if(associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }
            return new List<ToGnuCash>();
        }
        #endregion
        #region Helpers
        private decimal FindFixedFeesRefunded(List<MarketplaceFee> marketplaceFees)
        {
            if (marketplaceFees == null)
            {
                return 0m;
            }
            return marketplaceFees.Where(f => f.FeeType == FeeTypeEnum.FINAL_VALUE_FEE_FIXED_PER_ORDER)
                                    .Sum(f => f.Amount.DollarAmount() ?? 0m);
        }



        private decimal FindInternationalFeesRefunded(List<MarketplaceFee> marketplaceFees)
        {
            if (marketplaceFees == null)
            {
                return 0m;
            }
            return marketplaceFees.Where(f => f.FeeType == FeeTypeEnum.INTERNATIONAL_FEE)
                                    .Sum(f => f.Amount.DollarAmount() ?? 0m);
        }
        private decimal FindVariableFeesRefunded(List<MarketplaceFee> marketplaceFees)
        {
            if (marketplaceFees == null)
            {
                return 0m;
            }
            return marketplaceFees.Where(f => f.FeeType == FeeTypeEnum.FINAL_VALUE_FEE)
                                    .Sum(f => f.Amount.DollarAmount() ?? 0m);
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
        private List<ToGnuCash> HandlePartialShippingRefund(Transaction transaction, 
                                                            DateOnly refundDate, 
                                                            decimal partialRefundAmount, 
                                                            string feeBaySellerID, 
                                                            string orderId)
        {
            var outputData = new List<ToGnuCash>();
           //! income line - shipping
                var shippingIncomeLine = new ToGnuCash();
                shippingIncomeLine.Date = refundDate;
                shippingIncomeLine.Account = $"Income:{feeBaySellerID} Shipping";
                shippingIncomeLine.Description = $"feeBay Order #{orderId} Shipping Refund";//";
               // set the shipping line amount to the negative of the shipping price
                shippingIncomeLine.Amount = -partialRefundAmount ;
                shippingIncomeLine.TransactionId = transaction.TransactionId;//orderId;
                shippingIncomeLine.SortOrder = 1;
                outputData.Add(shippingIncomeLine);
                   //! variable fee line
                var variableFeeLine = new ToGnuCash();
                variableFeeLine.Date = refundDate;
                variableFeeLine.Account = $"Expenses:FeeBay Fees:{feeBaySellerID}:Final Value Fees";
                variableFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                // feeBay refunds the fees back to you so this is positive
                variableFeeLine.Amount = transaction.TotalFeeAmount.DollarAmount() ?? 0m;
                variableFeeLine.TransactionId = transaction.TransactionId;//orderId;
                variableFeeLine.SortOrder = 4;
                outputData.Add(variableFeeLine);

                //!remove the remaining money from feeBay current assett
                var netIncomeLine = new ToGnuCash();
                netIncomeLine.Date = refundDate;
                netIncomeLine.Account = $"Assets:Current Assets:feeBay:{feeBaySellerID}";
                netIncomeLine.Description = string.Empty;
                netIncomeLine.Amount =  transaction.Amount.DollarAmount() ?? 0m;
                netIncomeLine.TransactionId = transaction.TransactionId;//orderId;
                netIncomeLine.SortOrder = 5;
                outputData.Add(netIncomeLine);

                //!international fees line
                // if(internationalFeesRefunded != 0)
                // {
                //     var internationalFeeLine = new ToGnuCash();
                //     internationalFeeLine.Date = refundDate;
                //     internationalFeeLine.Account = $"Expenses:FeeBay Fees:{feeBaySellerID}:International Fee";
                //     internationalFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                //     internationalFeeLine.Amount = internationalFeesRefunded;
                //     internationalFeeLine.TransactionId = transaction.TransactionId;//orderId;
                //     internationalFeeLine.SortOrder = 6;
                //     outputData.Add(internationalFeeLine);
                // }
            
            return outputData;
        }

        private List<ToGnuCash> HandlePromotedListingsPriorityFees(Transaction transaction, string itemId)
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

            return outputData;
        }

        private List<ToGnuCash> HandleStoreSubscriptionCharge()
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
            return new List<ToGnuCash>();
        }
        private decimal MakeCogsForFullOrder(decimal sellingPrice, string sku)
        {
            decimal fullOrderCogs = 0.0m;
            if(sellingPrice == 130.0m)
            {
                return fullOrderCogs;
            }

            var singleStampCogs = _localDbConnectionManager.GetStampCOGS(sku);
            if(singleStampCogs == 0.0m)
            {
                singleStampCogs = -(sellingPrice / 2);
            } else
            {
                singleStampCogs = (decimal)-(singleStampCogs);
            }
            fullOrderCogs = (decimal)singleStampCogs;

            return fullOrderCogs;
        }

        private decimal SumUpRefunds(List<Refund> refunds)
        {
            if (refunds == null)
            {
                return 0m;
            }
            return (decimal)(refunds.Sum(r => r.Amount.DollarAmount() ?? 0m));
        }

        #endregion
        #endregion
        #endregion
    }
}
