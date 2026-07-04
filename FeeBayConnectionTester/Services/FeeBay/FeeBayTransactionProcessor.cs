using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances.Payout;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances.Transaction;
using EbaySharp.Entities.Develop.SellingApps.OrderManagement.Fulfillment.Order;
using FeeBayConnectionTester.DTO;
using FeeBayConnectionTester.Extensions;
using LocalDBConnections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FeeBayConnectionTester.Services.FeeBay
{

    public class FeeBayTransactionProcessor : IFeeBayTransactionProcessor
    {
        private readonly ILocalDbConnectionManager _localDbConnectionManager;
        private const string FeeBaySellerID = "Simmons Ink";

        public FeeBayTransactionProcessor(ILocalDbConnectionManager localDbConnectionManager)
        {
            _localDbConnectionManager = localDbConnectionManager;
        }

        public async Task<List<ToGnuCash>> ProcessTransactionsAsync(
            List<Payout> payoutList,
            List<Transaction> transactionList,
            List<Order> orderList)
        {
            // Validate inputs
            var transactionsWithoutPayout = transactionList
                .Where(t => string.IsNullOrWhiteSpace(t.PayoutId))
                .ToList();

            if (transactionsWithoutPayout.Any())
            {
                // Log or handle transactions without payout (would be shown via MessageBox in UI layer)
                System.Diagnostics.Debug.WriteLine(
                    $"Found {transactionsWithoutPayout.Count} transactions not associated with any payout.");
            }

            var results = new List<ToGnuCash>();
            var groupedTransactions = transactionList
                .Where(t => !string.IsNullOrWhiteSpace(t.PayoutId))
                .GroupBy(t => t.PayoutId!)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var payout in payoutList)
            {
                if (payout == null)
                {
                    throw new InvalidOperationException("No payouts found.");
                }

                var payoutId = payout.PayoutId;
                int? numTransactionsInPayout = payout.TransactionCount;

                if (!groupedTransactions.TryGetValue(payoutId, out var transactionsInThisPayout))
                {
                    continue;
                }

                if (transactionsInThisPayout.Count != numTransactionsInPayout)
                {
                    // Only process completed payouts
                    continue;
                }

                // Add the payout itself
                var payoutLines = HandlePayout(payout);
                results.AddRange(payoutLines);

                // Process transactions within this payout
                foreach (var transaction in transactionsInThisPayout)
                {
                    var associatedOrder = orderList
                        .FirstOrDefault(o => o.OrderId == transaction.OrderId);

                    var transactionLines = ProcessTransactionByType(
                        transaction,
                        associatedOrder);

                    results.AddRange(transactionLines);
                }
            }

            return results;
        }

        private List<ToGnuCash> ProcessTransactionByType(
            Transaction transaction,
            Order? associatedOrder)
        {
            return transaction.TransactionType switch
            {
                TransactionTypeEnum.SALE =>
                    HandleSale(transaction, associatedOrder),

                TransactionTypeEnum.REFUND =>
                    HandleRefundByStatus(transaction, associatedOrder),

                TransactionTypeEnum.CREDIT =>
                    HandleCredit(associatedOrder),

                TransactionTypeEnum.DISPUTE =>
                    HandleDispute(associatedOrder),

                TransactionTypeEnum.SHIPPING_LABEL =>
                    HandleShipping_Label(transaction),

                TransactionTypeEnum.TRANSFER =>
                    HandleTransfer(associatedOrder),

                TransactionTypeEnum.NON_SALE_CHARGE =>
                    HandleNon_Sale_Charge(transaction),

                TransactionTypeEnum.ADJUSTMENT =>
                    HandleAdjustment(associatedOrder),

                TransactionTypeEnum.WITHDRAWAL =>
                    HandleWithdrawal(associatedOrder),

                TransactionTypeEnum.LOAN_REPAYMENT =>
                    HandleLoan_Repayment(associatedOrder),

                TransactionTypeEnum.PURCHASE =>
                    HandlePurchase(associatedOrder),

                null or _ => new List<ToGnuCash>()
            };
        }

        private List<ToGnuCash> HandleRefundByStatus(
            Transaction transaction,
            Order? associatedOrder)
        {
            if (associatedOrder?.OrderPaymentStatus == OrderPaymentStatusEnum.FULLY_REFUNDED)
            {
                return HandleFullRefund(transaction, associatedOrder);
            }

            if (associatedOrder?.OrderPaymentStatus == OrderPaymentStatusEnum.PARTIALLY_REFUNDED)
            {
                return HandlePartialRefund(transaction, associatedOrder);
            }

            throw new NotSupportedException(
                $"Unsupported order payment status in REFUND: {associatedOrder?.OrderPaymentStatus}");
        }

        #region Transaction Handlers

        private List<ToGnuCash> HandlePayout(Payout payout)
        {
            var outputData = new List<ToGnuCash>();
            var payoutLineFrom = new ToGnuCash();
            payoutLineFrom.Account = $"Assets:Current Assets:feeBay:{FeeBaySellerID}";
            payoutLineFrom.Date = DateOnly.FromDateTime(DateTime.Parse(payout.PayoutDate));
            payoutLineFrom.Description = $"{payout.PayoutMemo} {payout.PayoutId}";
            payoutLineFrom.Amount = payout.Amount.DollarAmount() ?? 0m;
            payoutLineFrom.TransactionId = payout.PayoutId;
            payoutLineFrom.SortOrder = 1;
            outputData.Add(payoutLineFrom);

            var payoutLineTo = new ToGnuCash();
            payoutLineTo.Account = "Assets:Current Assets:TCCU Business Checking";
            payoutLineTo.Date = DateOnly.FromDateTime(DateTime.Parse(payout.PayoutDate));
            payoutLineTo.Description = string.Empty;
            payoutLineTo.Amount = -payout.Amount.DollarAmount() ?? 0m;
            payoutLineTo.TransactionId = payout.PayoutId;
            payoutLineTo.SortOrder = 2;
            outputData.Add(payoutLineTo);

            return outputData;
        }

        private List<ToGnuCash> HandleSale(Transaction transaction, Order? associatedOrder)
        {
            if (associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }

            if (transaction.OrderLineItems == null || !transaction.OrderLineItems.Any())
            {
                return new List<ToGnuCash>();
            }

            var outputData = new List<ToGnuCash>();

            foreach (var transactionLineItem in transaction.OrderLineItems)
            {
                var orderLineItem = associatedOrder.LineItems
                    .FirstOrDefault(l => l.LineItemId == transactionLineItem.LineItemId);

                if (orderLineItem == null)
                    continue;

                var itemFees = transactionLineItem.MarketplaceFees;
                var skusInOrder = orderLineItem.SKU;
                var orderDate = associatedOrder.CreationDate;
                var orderId = transaction.OrderId;
                var transactionId = transaction.TransactionId;
                var sellingPrice = orderLineItem.LineItemCost.DollarAmount() ?? 0m;
                var shippingPrice = orderLineItem.DeliveryCost.ShippingCost.DollarAmount() ?? 0m;
                var fixedFees = itemFees.SingleOrDefault(f => f.FeeType == FeeTypeEnum.FINAL_VALUE_FEE_FIXED_PER_ORDER)?.Amount?.DollarAmount() ?? 0m;
                var variableFees = itemFees.SingleOrDefault(f => f.FeeType == FeeTypeEnum.FINAL_VALUE_FEE)?.Amount?.DollarAmount() ?? 0m;
                var internationalFees = itemFees.SingleOrDefault(f => f.FeeType == FeeTypeEnum.INTERNATIONAL_FEE)?.Amount?.DollarAmount() ?? 0m;
                var gross = orderLineItem.Total.DollarAmount() ?? 0m;
                var net = gross - fixedFees - variableFees - internationalFees;
                var title = orderLineItem.Title;
                var incomeLineDescription = $"feeBay Order #{orderId} SKU: {skusInOrder} - {title} ";

                outputData.AddRange(CreateSaleLineItems(
                    incomeLineDescription,
                    orderDate,
                    transactionId,
                    sellingPrice,
                    shippingPrice,
                    fixedFees,
                    variableFees,
                    internationalFees,
                    net,
                    MakeCogsForFullOrder(sellingPrice, skusInOrder)));
            }

            return outputData;
        }

        private List<ToGnuCash> CreateSaleLineItems(
            string description,
            string orderDate,
            string transactionId,
            decimal sellingPrice,
            decimal shippingPrice,
            decimal fixedFees,
            decimal variableFees,
            decimal internationalFees,
            decimal net,
            decimal cogs)
        {
            var outputData = new List<ToGnuCash>();
            var orderDateOnly = DateOnly.FromDateTime(DateTime.Parse(orderDate));

            // Income line - product
            outputData.Add(new ToGnuCash
            {
                Date = orderDateOnly,
                Account = $"Income:{FeeBaySellerID} Sales",
                Description = description,
                Amount = sellingPrice,
                TransactionId = transactionId,
                SortOrder = 1
            });

            // Income line - shipping
            outputData.Add(new ToGnuCash
            {
                Date = orderDateOnly,
                Account = $"Income:{FeeBaySellerID} Shipping",
                Description = string.Empty,
                Amount = shippingPrice,
                TransactionId = transactionId,
                SortOrder = 2
            });

            // Fixed fee line
            outputData.Add(new ToGnuCash
            {
                Date = orderDateOnly,
                Account = $"Expences:FeeBay Fees:{FeeBaySellerID}:Fixed Fee Per Sale",
                Description = string.Empty,
                Amount = -fixedFees,
                TransactionId = transactionId,
                SortOrder = 3
            });

            // Variable fee line
            outputData.Add(new ToGnuCash
            {
                Date = orderDateOnly,
                Account = $"Expenses:FeeBay Fees:{FeeBaySellerID}:Final Value Fees",
                Description = string.Empty,
                Amount = -variableFees,
                TransactionId = transactionId,
                SortOrder = 4
            });

            // Net income line
            outputData.Add(new ToGnuCash
            {
                Date = orderDateOnly,
                Account = $"Assets:Current Assets:feeBay:{FeeBaySellerID}",
                Description = string.Empty,
                Amount = -net,
                TransactionId = transactionId,
                SortOrder = 5
            });

            // International fees line (if applicable)
            if (internationalFees != 0)
            {
                outputData.Add(new ToGnuCash
                {
                    Date = orderDateOnly,
                    Account = $"Expenses:FeeBay Fees:{FeeBaySellerID}:International Fee",
                    Description = string.Empty,
                    Amount = -internationalFees,
                    TransactionId = transactionId,
                    SortOrder = 6
                });
            }

            // COGS line
            outputData.Add(new ToGnuCash
            {
                Date = orderDateOnly,
                Account = "Expenses:Cost of Goods Sold",
                Description = string.Empty,
                Amount = cogs,
                TransactionId = transactionId,
                SortOrder = 7
            });

            // Inventory line
            outputData.Add(new ToGnuCash
            {
                Date = orderDateOnly,
                Account = "Assets:INVENTORY",
                Description = string.Empty,
                Amount = -cogs,
                TransactionId = transactionId,
                SortOrder = 8
            });

            return outputData;
        }

        private List<ToGnuCash> HandleFullRefund(Transaction transaction, Order? associatedOrder)
        {
            if (associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }

            var outputData = new List<ToGnuCash>();
            var refundDate = DateOnly.FromDateTime(DateTime.Parse(transaction.TransactionDate));

            foreach (var transactionLineItem in transaction.OrderLineItems)
            {
                var orderLineItem = associatedOrder.LineItems
                    .FirstOrDefault(l => l.LineItemId == transactionLineItem.LineItemId);

                if (orderLineItem == null)
                    continue;

                var totalRefund = SumUpRefunds(orderLineItem.Refunds);
                var fixedFeesRefunded = FindFixedFeesRefunded(transactionLineItem.MarketplaceFees);
                var variableFeesRefunded = FindVariableFeesRefunded(transactionLineItem.MarketplaceFees);
                var internationalFeesRefunded = FindInternationalFeesRefunded(transactionLineItem.MarketplaceFees);
                var netRefund = totalRefund - fixedFeesRefunded - variableFeesRefunded - internationalFeesRefunded;

                var sellingPriceRefunded = orderLineItem.LineItemCost.DollarAmount() ?? 0m;
                var shippingPriceRefunded = orderLineItem.DeliveryCost.ShippingCost.DollarAmount() ?? 0m;
                var skusInOrder = orderLineItem.SKU;
                var title = orderLineItem.Title;
                var orderId = transaction.OrderId;
                var incomeLineDescription = $"feeBay Order #{orderId} SKU: {skusInOrder} - {title} - REFUND";

                outputData.AddRange(CreateRefundLineItems(
                    incomeLineDescription,
                    refundDate,
                    transaction.TransactionId,
                    sellingPriceRefunded,
                    shippingPriceRefunded,
                    fixedFeesRefunded,
                    variableFeesRefunded,
                    internationalFeesRefunded,
                    netRefund,
                    MakeCogsForFullOrder(sellingPriceRefunded, skusInOrder)));
            }

            return outputData;
        }

        private List<ToGnuCash> CreateRefundLineItems(
            string description,
            DateOnly refundDate,
            string transactionId,
            decimal sellingPriceRefunded,
            decimal shippingPriceRefunded,
            decimal fixedFeesRefunded,
            decimal variableFeesRefunded,
            decimal internationalFeesRefunded,
            decimal netRefund,
            decimal cogs)
        {
            var outputData = new List<ToGnuCash>();

            // Income line - product (negative because it's a refund)
            outputData.Add(new ToGnuCash
            {
                Date = refundDate,
                Account = $"Income:{FeeBaySellerID} Sales",
                Description = description,
                Amount = -sellingPriceRefunded,
                TransactionId = transactionId,
                SortOrder = 1
            });

            // Income line - shipping
            outputData.Add(new ToGnuCash
            {
                Date = refundDate,
                Account = $"Income:{FeeBaySellerID} Shipping",
                Description = string.Empty,
                Amount = -shippingPriceRefunded,
                TransactionId = transactionId,
                SortOrder = 2
            });

            // Fixed fee line (positive because fees are refunded)
            outputData.Add(new ToGnuCash
            {
                Date = refundDate,
                Account = $"Expences:FeeBay Fees:{FeeBaySellerID}:Fixed Fee Per Sale",
                Description = string.Empty,
                Amount = fixedFeesRefunded,
                TransactionId = transactionId,
                SortOrder = 3
            });

            // Variable fee line
            outputData.Add(new ToGnuCash
            {
                Date = refundDate,
                Account = $"Expenses:FeeBay Fees:{FeeBaySellerID}:Final Value Fees",
                Description = string.Empty,
                Amount = variableFeesRefunded,
                TransactionId = transactionId,
                SortOrder = 4
            });

            // Net line
            outputData.Add(new ToGnuCash
            {
                Date = refundDate,
                Account = $"Assets:Current Assets:feeBay:{FeeBaySellerID}",
                Description = string.Empty,
                Amount = netRefund,
                TransactionId = transactionId,
                SortOrder = 5
            });

            // International fees line
            if (internationalFeesRefunded != 0)
            {
                outputData.Add(new ToGnuCash
                {
                    Date = refundDate,
                    Account = $"Expenses:FeeBay Fees:{FeeBaySellerID}:International Fee",
                    Description = string.Empty,
                    Amount = internationalFeesRefunded,
                    TransactionId = transactionId,
                    SortOrder = 6
                });
            }

            // COGS line (reversed)
            outputData.Add(new ToGnuCash
            {
                Date = refundDate,
                Account = "Expenses:Cost of Goods Sold",
                Description = string.Empty,
                Amount = -cogs,
                TransactionId = transactionId,
                SortOrder = 7
            });

            // Inventory line (reversed)
            outputData.Add(new ToGnuCash
            {
                Date = refundDate,
                Account = "Assets:INVENTORY",
                Description = string.Empty,
                Amount = cogs,
                TransactionId = transactionId,
                SortOrder = 8
            });

            return outputData;
        }

        private List<ToGnuCash> HandlePartialRefund(Transaction transaction, Order? associatedOrder)
        {
            if (associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }

            var refundDate = DateOnly.FromDateTime(DateTime.Parse(transaction.TransactionDate));
            var totalShippingCharges = associatedOrder.LineItems.Sum(li => li.DeliveryCost.ShippingCost.DollarAmount() ?? 0m);
            var partialRefundAmount = (transaction.Amount.DollarAmount() ?? 0m) + (transaction.TotalFeeAmount.DollarAmount() ?? 0m);

            if (transaction.Amount.DollarAmount() < totalShippingCharges)
            {
                return HandlePartialShippingRefund(transaction, refundDate, partialRefundAmount);
            }

            return HandlePartialProductRefund(transaction, associatedOrder);
        }

        private List<ToGnuCash> HandlePartialShippingRefund(
            Transaction transaction,
            DateOnly refundDate,
            decimal partialRefundAmount)
        {
            var outputData = new List<ToGnuCash>();

            // Shipping income line
            outputData.Add(new ToGnuCash
            {
                Date = refundDate,
                Account = $"Income:{FeeBaySellerID} Shipping",
                Description = $"feeBay Order #{transaction.OrderId} Shipping Refund",
                Amount = -partialRefundAmount,
                TransactionId = transaction.TransactionId,
                SortOrder = 1
            });

            // Fee line
            outputData.Add(new ToGnuCash
            {
                Date = refundDate,
                Account = $"Expenses:FeeBay Fees:{FeeBaySellerID}:Final Value Fees",
                Description = string.Empty,
                Amount = transaction.TotalFeeAmount.DollarAmount() ?? 0m,
                TransactionId = transaction.TransactionId,
                SortOrder = 4
            });

            // Net line
            outputData.Add(new ToGnuCash
            {
                Date = refundDate,
                Account = $"Assets:Current Assets:feeBay:{FeeBaySellerID}",
                Description = string.Empty,
                Amount = transaction.Amount.DollarAmount() ?? 0m,
                TransactionId = transaction.TransactionId,
                SortOrder = 5
            });

            return outputData;
        }

        private List<ToGnuCash> HandlePartialProductRefund(Transaction transaction, Order associatedOrder)
        {
            var outputData = new List<ToGnuCash>();
            var refundDate = DateOnly.FromDateTime(DateTime.Parse(transaction.TransactionDate));

            foreach (var transactionLineItem in transaction.OrderLineItems)
            {
                var orderLineItem = associatedOrder.LineItems
                    .FirstOrDefault(l => l.LineItemId == transactionLineItem.LineItemId);

                if (orderLineItem == null)
                    continue;

                var lineItemRefund = SumUpRefunds(orderLineItem.Refunds);
                var fixedFeesRefunded = FindFixedFeesRefunded(transactionLineItem.MarketplaceFees);
                var variableFeesRefunded = FindVariableFeesRefunded(transactionLineItem.MarketplaceFees);
                var internationalFeesRefunded = FindInternationalFeesRefunded(transactionLineItem.MarketplaceFees);
                var netRefund = lineItemRefund - fixedFeesRefunded - variableFeesRefunded - internationalFeesRefunded;

                var skusInOrder = orderLineItem.SKU;
                var title = orderLineItem.Title;
                var orderId = associatedOrder.OrderId;
                var incomeLineDescription = $"feeBay Order #{orderId} SKU: {skusInOrder} - {title} - REFUND";

                // Income line
                outputData.Add(new ToGnuCash
                {
                    Date = refundDate,
                    Account = $"Income:{FeeBaySellerID} Sales",
                    Description = incomeLineDescription,
                    Amount = -lineItemRefund,
                    TransactionId = transaction.TransactionId,
                    SortOrder = 1
                });

                // Fee lines
                if (fixedFeesRefunded != 0)
                {
                    outputData.Add(new ToGnuCash
                    {
                        Date = refundDate,
                        Account = $"Expences:FeeBay Fees:{FeeBaySellerID}:Fixed Fee Per Sale",
                        Description = string.Empty,
                        Amount = -fixedFeesRefunded,
                        TransactionId = transaction.TransactionId,
                        SortOrder = 3
                    });
                }

                outputData.Add(new ToGnuCash
                {
                    Date = refundDate,
                    Account = $"Expenses:FeeBay Fees:{FeeBaySellerID}:Final Value Fees",
                    Description = string.Empty,
                    Amount = variableFeesRefunded,
                    TransactionId = transaction.TransactionId,
                    SortOrder = 4
                });

                // Net line
                outputData.Add(new ToGnuCash
                {
                    Date = refundDate,
                    Account = $"Assets:Current Assets:feeBay:{FeeBaySellerID}",
                    Description = string.Empty,
                    Amount = netRefund,
                    TransactionId = transaction.TransactionId,
                    SortOrder = 5
                });

                // International fees
                if (internationalFeesRefunded != 0)
                {
                    outputData.Add(new ToGnuCash
                    {
                        Date = refundDate,
                        Account = $"Expenses:FeeBay Fees:{FeeBaySellerID}:International Fee",
                        Description = string.Empty,
                        Amount = internationalFeesRefunded,
                        TransactionId = transaction.TransactionId,
                        SortOrder = 6
                    });
                }
            }

            return outputData;
        }

        private List<ToGnuCash> HandleNon_Sale_Charge(Transaction transaction)
        {
            var feeTypes = transaction.References;
            if (feeTypes == null)
            {
                return HandleStoreSubscriptionCharge(transaction);
            }

            foreach (var fee in feeTypes)
            {
                if (fee.ReferenceType == ReferenceTypeEnum.ITEM_ID && 
                    transaction.TransactionMemo == "Promoted Listings - Priority fee")
                {
                    return HandlePromotedListingsPriorityFees(transaction, fee.ReferenceId);
                }
            }

            throw new NotImplementedException();
        }

        private List<ToGnuCash> HandlePromotedListingsPriorityFees(Transaction transaction, string itemId)
        {
            var outputData = new List<ToGnuCash>();
            var transactionDate = DateOnly.FromDateTime(DateTime.Parse(transaction.TransactionDate));

            outputData.Add(new ToGnuCash
            {
                Date = transactionDate,
                Account = $"Assets:Current Assets:feeBay:{FeeBaySellerID}",
                Description = $"{transaction.TransactionMemo} ItemId {itemId}",
                Amount = transaction.Amount.DollarAmount() ?? 0m,
                TransactionId = transaction.TransactionId,
                SortOrder = 1
            });

            outputData.Add(new ToGnuCash
            {
                Date = transactionDate,
                Account = $"Expenses:FeeBay Fees:{FeeBaySellerID}:Promoted Listings Fee",
                Description = string.Empty,
                Amount = -transaction.Amount.DollarAmount() ?? 0m,
                TransactionId = transaction.TransactionId,
                SortOrder = 2
            });

            return outputData;
        }

        private List<ToGnuCash> HandleStoreSubscriptionCharge(Transaction transaction)
        {
            var outputData = new List<ToGnuCash>();
            var transactionDate = DateOnly.FromDateTime(DateTime.Parse(transaction.TransactionDate));

            outputData.Add(new ToGnuCash
            {
                Date = transactionDate,
                Account = $"Assets:Current Assets:feeBay:{FeeBaySellerID}",
                Description = $"feeBay store monthly charge {transaction.TransactionMemo}",
                Amount = transaction.Amount.DollarAmount() ?? 0m,
                TransactionId = transaction.TransactionId,
                SortOrder = 1
            });

            outputData.Add(new ToGnuCash
            {
                Date = transactionDate,
                Account = $"Expenses:FeeBay Fees:{FeeBaySellerID}:Store Monthly Fee",
                Description = string.Empty,
                Amount = -transaction.Amount.DollarAmount() ?? 0m,
                TransactionId = transaction.TransactionId,
                SortOrder = 2
            });

            return outputData;
        }

        private List<ToGnuCash> HandleShipping_Label(Transaction label)
        {
            var outputData = new List<ToGnuCash>();
            var labelDate = DateOnly.FromDateTime(DateTime.Parse(label.TransactionDate));

            outputData.Add(new ToGnuCash
            {
                Date = labelDate,
                Account = $"Assets:Current Assets:feeBay:{FeeBaySellerID}",
                Description = $"{label.OrderId} - {label.TransactionMemo}",
                Amount = label.Amount.DollarAmount() ?? 0m,
                TransactionId = label.TransactionId,
                SortOrder = 1
            });

            outputData.Add(new ToGnuCash
            {
                Date = labelDate,
                Account = "Expenses:Postage and Delivery",
                Description = string.Empty,
                Amount = -label.Amount.DollarAmount() ?? 0m,
                TransactionId = label.TransactionId,
                SortOrder = 2
            });

            return outputData;
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

        private List<ToGnuCash> HandleTransfer(Order? associatedOrder)
        {
            if (associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }
            return new List<ToGnuCash>();
        }

        private List<ToGnuCash> HandleAdjustment(Order? associatedOrder)
        {
            if (associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }
            return new List<ToGnuCash>();
        }

        private List<ToGnuCash> HandleWithdrawal(Order? associatedOrder)
        {
            if (associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }
            return new List<ToGnuCash>();
        }

        private List<ToGnuCash> HandleLoan_Repayment(Order? associatedOrder)
        {
            if (associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }
            return new List<ToGnuCash>();
        }

        private List<ToGnuCash> HandlePurchase(Order? associatedOrder)
        {
            if (associatedOrder == null)
            {
                throw new ArgumentNullException(nameof(associatedOrder));
            }
            return new List<ToGnuCash>();
        }

        #endregion

        #region Helper Methods

        private decimal FindFixedFeesRefunded(List<MarketplaceFee> marketplaceFees)
        {
            if (marketplaceFees == null)
            {
                return 0m;
            }
            return marketplaceFees
                .Where(f => f.FeeType == FeeTypeEnum.FINAL_VALUE_FEE_FIXED_PER_ORDER)
                .Sum(f => f.Amount.DollarAmount() ?? 0m);
        }

        private decimal FindVariableFeesRefunded(List<MarketplaceFee> marketplaceFees)
        {
            if (marketplaceFees == null)
            {
                return 0m;
            }
            return marketplaceFees
                .Where(f => f.FeeType == FeeTypeEnum.FINAL_VALUE_FEE)
                .Sum(f => f.Amount.DollarAmount() ?? 0m);
        }

        private decimal FindInternationalFeesRefunded(List<MarketplaceFee> marketplaceFees)
        {
            if (marketplaceFees == null)
            {
                return 0m;
            }
            return marketplaceFees
                .Where(f => f.FeeType == FeeTypeEnum.INTERNATIONAL_FEE)
                .Sum(f => f.Amount.DollarAmount() ?? 0m);
        }

        private decimal MakeCogsForFullOrder(decimal sellingPrice, string sku)
        {
            if (sellingPrice == 130.0m)
            {
                return 0.0m;
            }

            var singleStampCogs = _localDbConnectionManager.GetStampCOGS(sku);
            if (singleStampCogs == 0.0m)
            {
                singleStampCogs = -(sellingPrice / 2);
            }
            else
            {
                singleStampCogs = (decimal)-singleStampCogs;
            }

            return singleStampCogs;
        }

        private decimal SumUpRefunds(List<Refund> refunds)
        {
            if (refunds == null)
            {
                return 0m;
            }
            return refunds.Sum(r => r.Amount.DollarAmount() ?? 0m);
        }

        #endregion
    }
}
