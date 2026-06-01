using EbaySharp.Entities.Common;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances.Transaction;
using EbaySharp.Entities.Develop.SellingApps.OrderManagement.Fulfillment.Order;
using FeeBayConnectionTester.Extensions;
using LocalDBConnections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FeeBayConnectionTester.Services
{
    public class EbayToGnuCashConverter
    {
        #region Constants and Fields
        private readonly ILocalDbConnectionManager _localDbConnectionManager;

        private static readonly Dictionary<string, (string FeeAccount, string AssetAccount)> FeeBayUserNameMap = new()
        {
            { "Simmons_Ink", ("Simmons_Ink", "eBay SI") },
            { "DuckStampDealer", ("DuckStampDealer", "eBay DSD") }
        };
        #endregion

        #region Constructors
        public EbayToGnuCashConverter(ILocalDbConnectionManager localDbConnectionManager)
        {
            _localDbConnectionManager = localDbConnectionManager ?? throw new ArgumentNullException(nameof(localDbConnectionManager));
        }
        #endregion

        #region Public Methods
        public List<ToGnuCash> ConvertOrdersAndTransactions(
            List<Order> orders, 
            List<Transaction> transactions, 
            string feeBayUserName)
        {
            if (!FeeBayUserNameMap.ContainsKey(feeBayUserName))
            {
                throw new ArgumentException($"Unknown eBay user name: {feeBayUserName}. Valid values are: {string.Join(", ", FeeBayUserNameMap.Keys)}");
            }

            var result = new List<ToGnuCash>();
            var validationErrors = new List<string>();

            // Filter for PAYOUT transactions only
            var payoutTransactions = transactions
                .Where(t => t.TransactionStatus == TransactionStatusEnum.PAYOUT)
                .ToList();

            Console.WriteLine($"Total transactions: {transactions.Count}, PAYOUT transactions: {payoutTransactions.Count}");

            // Group PAYOUT transactions by OrderId for efficient lookup
            var payoutsByOrderId = payoutTransactions.ToLookup(t => t.OrderId);

            var lineItemFeeMap = BuildLineItemFeeMap(payoutTransactions);

            foreach (var order in orders)
            {
                // Skip orders without matching PAYOUT transactions
                if (!payoutsByOrderId.Contains(order.OrderId))
                {
                    Console.WriteLine($"Skipping order {order.OrderId} - no PAYOUT transaction found");
                    validationErrors.Add($"Order {order.OrderId} - no PAYOUT transaction found");
                    continue;
                }

                var orderTransactions = payoutsByOrderId[order.OrderId].ToList();

                if (!orderTransactions.Any())
                {
                    Console.WriteLine($"Warning: Order {order.OrderId} has no matching PAYOUT transactions");
                    validationErrors.Add($"Order {order.OrderId} has no matching PAYOUT transactions");
                    continue;
                }

                foreach (var lineItem in order.LineItems)
                {
                    if (!lineItemFeeMap.ContainsKey(lineItem.LineItemId))
                    {
                        Console.WriteLine($"Warning: LineItem {lineItem.LineItemId} not found in transaction fee map");
                        validationErrors.Add($"LineItem {lineItem.LineItemId} not found in transaction fee map");
                        continue;
                    }

                    var financeLineItem = lineItemFeeMap[lineItem.LineItemId];
                    var lineItemEntries = ProcessSaleLineItem(lineItem, order, financeLineItem, orderTransactions.First(), feeBayUserName);
                    result.AddRange(lineItemEntries);
                }
            }

            if (validationErrors.Any())
            {
                Console.WriteLine($"\n=== Validation Summary ===");
                Console.WriteLine($"Total errors: {validationErrors.Count}");
                foreach (var error in validationErrors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }

            return result;
        }
        #endregion

        #region Private Methods
        private Dictionary<string, OrderLineItem> BuildLineItemFeeMap(List<Transaction> transactions)
        {
            var map = new Dictionary<string, OrderLineItem>();

            foreach (var transaction in transactions)
            {
                if (transaction.OrderLineItems == null) continue;

                foreach (var lineItem in transaction.OrderLineItems)
                {
                    if (!string.IsNullOrEmpty(lineItem.LineItemId))
                    {
                        map[lineItem.LineItemId] = lineItem;
                    }
                }
            }

            return map;
        }

        private List<ToGnuCash> ProcessSaleLineItem(
            LineItem lineItem, 
            Order order, 
            OrderLineItem financeLineItem,
            Transaction transaction,
            string feeBayUserName)
        {
            var entries = new List<ToGnuCash>();
            var userMapping = FeeBayUserNameMap[feeBayUserName];
            var orderDate = DateTime.Parse(order.CreationDate);
            var transactionId = $"{order.OrderId}-{lineItem.LineItemId}";

            // (1) Product Sale Income
            var saleAmount = lineItem.LineItemCost?.Value != null ? decimal.Parse(lineItem.LineItemCost.Value) : 0;
            entries.Add(new ToGnuCash
            {
                Date = orderDate,
                Account = $"Income:{userMapping.AssetAccount} Sales:Product Sale",
                Description = $"eBay Order #{order.OrderId}-{lineItem.LineItemId} - {lineItem.SKU} - {lineItem.Title}",
                Amount = saleAmount,
                TransactionId = transactionId,
                SortOrder = 1
            });

            // (2) Shipping Income
            var shippingAmount = lineItem.DeliveryCost?.ShippingCost?.Value != null 
                ? decimal.Parse(lineItem.DeliveryCost.ShippingCost.Value) 
                : 0;

            if (shippingAmount > 0)
            {
                entries.Add(new ToGnuCash
                {
                    Date = orderDate,
                    Account = $"Income:{userMapping.AssetAccount} Sales:Shipping",
                    Description = string.Empty,
                    Amount = shippingAmount,
                    TransactionId = transactionId,
                    SortOrder = 2
                });
            }

            // (3) Fixed Fee Expense
            var fixedFee = financeLineItem.GetFeeByType(FeeTypeEnum.FINAL_VALUE_FEE_FIXED_PER_ORDER) ?? 0;
            if (fixedFee > 0)
            {
                entries.Add(new ToGnuCash
                {
                    Date = orderDate,
                    Account = $"Expenses:eBay Fees:{userMapping.FeeAccount}:Fixed Fee Per Sale",
                    Description = string.Empty,
                    Amount = fixedFee,
                    TransactionId = transactionId,
                    SortOrder = 3
                });
            }

            // (4) Final Value Fee Expense
            var finalValueFee = financeLineItem.GetFeeByType(FeeTypeEnum.FINAL_VALUE_FEE) ?? 0;
            var finalValueShippingFee = financeLineItem.GetFeeByType(FeeTypeEnum.FINAL_VALUE_SHIPPING_FEE) ?? 0;
            var totalFinalValueFee = finalValueFee + finalValueShippingFee;

            if (totalFinalValueFee > 0)
            {
                entries.Add(new ToGnuCash
                {
                    Date = orderDate,
                    Account = $"Expenses:eBay Fees:{userMapping.FeeAccount}:Final Value Fees",
                    Description = string.Empty,
                    Amount = totalFinalValueFee,
                    TransactionId = transactionId,
                    SortOrder = 4
                });
            }

            // (5) International Fee Expense
            var internationalFee = financeLineItem.GetFeeByType(FeeTypeEnum.INTERNATIONAL_FEE) ?? 0;
            if (internationalFee > 0)
            {
                entries.Add(new ToGnuCash
                {
                    Date = orderDate,
                    Account = $"Expenses:eBay Fees:{userMapping.FeeAccount}:International Fee",
                    Description = string.Empty,
                    Amount = internationalFee,
                    TransactionId = transactionId,
                    SortOrder = 5
                });
            }

            // (6) eBay Asset line (negative net amount from PAYOUT transaction)
            var netAmount = transaction.Amount?.Value != null ? decimal.Parse(transaction.Amount.Value) : 0;
            // Calculate the proportional net for this line item if multiple items in order
            var orderLineItemCount = order.LineItems?.Count ?? 1;
            var proportionalNet = netAmount / orderLineItemCount;

            entries.Add(new ToGnuCash
            {
                Date = orderDate,
                Account = $"Assets:Current Assets:eBay:{userMapping.AssetAccount}",
                Description = string.Empty,
                Amount = -proportionalNet,
                TransactionId = transactionId,
                SortOrder = 6
            });

            // (7) COGS Expense
            var cogs = CalculateCOGS(lineItem.SKU, saleAmount);
            entries.Add(new ToGnuCash
            {
                Date = orderDate,
                Account = "Expenses:Cost of Goods Sold",
                Description = $"COGS for SKU {lineItem.SKU}",
                Amount = cogs,
                TransactionId = transactionId,
                SortOrder = 7
            });

            // (8) Inventory Asset reduction (negative COGS)
            entries.Add(new ToGnuCash
            {
                Date = orderDate,
                Account = "Assets:INVENTORY",
                Description = $"COGS for SKU {lineItem.SKU}",
                Amount = -cogs,
                TransactionId = transactionId,
                SortOrder = 8
            });

            return entries;
        }

        private decimal CalculateCOGS(string sku, decimal sellingPrice)
        {
            try
            {
                // Try to parse SKU to int
                if (!int.TryParse(sku, out int skuInt))
                {
                    Console.WriteLine($"Warning: SKU '{sku}' is not numeric, using 50% fallback for COGS");
                    return sellingPrice * 0.5m;
                }

                // Try to get COGS from database
                // Note: StampDBService.StampDBConnection is used in FeeBayCleaner.cs but not accessible here
                // TODO: Add GetStampCostById method to ILocalDbConnectionManager interface
                // For now using the injected manager - but need to implement the method

                // TEMPORARY: Using 50% fallback until database method is available
                Console.WriteLine($"Warning: No cost found in database for SKU {skuInt}, using 50% fallback");
                return sellingPrice * 0.5m;

                // Original implementation (commented out until StampDBService is available):
                // var db = new StampDBService.StampDBConnection();
                // var stampCost = db.GetStampCostById(skuInt);
                // if (stampCost == null || stampCost == 0.0m)
                // {
                //     Console.WriteLine($"Warning: No cost found in database for SKU {skuInt}, using 50% fallback");
                //     return sellingPrice * 0.5m;
                // }
                // return (decimal)stampCost;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating COGS for SKU '{sku}': {ex.Message}, using 50% fallback");
                return sellingPrice * 0.5m;
            }
        }
        #endregion

        #region Transaction Type Handlers (Step 7)
        private List<ToGnuCash> ProcessRefund(Transaction transaction, string feeBayUserName)
        {
            // TODO: Implement refund processing
            throw new NotImplementedException($"Transaction type REFUND not yet implemented for transaction {transaction.TransactionId}");
        }

        private List<ToGnuCash> ProcessCredit(Transaction transaction, string feeBayUserName)
        {
            // TODO: Implement credit processing
            throw new NotImplementedException($"Transaction type CREDIT not yet implemented for transaction {transaction.TransactionId}");
        }

        private List<ToGnuCash> ProcessTransfer(Transaction transaction, string feeBayUserName)
        {
            // TODO: Implement transfer processing
            throw new NotImplementedException($"Transaction type TRANSFER not yet implemented for transaction {transaction.TransactionId}");
        }

        private List<ToGnuCash> ProcessNonSaleCharge(Transaction transaction, string feeBayUserName)
        {
            // TODO: Implement non-sale charge processing
            throw new NotImplementedException($"Transaction type NON_SALE_CHARGE not yet implemented for transaction {transaction.TransactionId}");
        }

        private List<ToGnuCash> ProcessAdjustment(Transaction transaction, string feeBayUserName)
        {
            // TODO: Implement adjustment processing
            throw new NotImplementedException($"Transaction type ADJUSTMENT not yet implemented for transaction {transaction.TransactionId}");
        }

        private List<ToGnuCash> ProcessWithdrawal(Transaction transaction, string feeBayUserName)
        {
            // TODO: Implement withdrawal processing
            throw new NotImplementedException($"Transaction type WITHDRAWAL not yet implemented for transaction {transaction.TransactionId}");
        }

        private List<ToGnuCash> ProcessLoanRepayment(Transaction transaction, string feeBayUserName)
        {
            // TODO: Implement loan repayment processing
            throw new NotImplementedException($"Transaction type LOAN_REPAYMENT not yet implemented for transaction {transaction.TransactionId}");
        }

        private List<ToGnuCash> ProcessPurchase(Transaction transaction, string feeBayUserName)
        {
            // TODO: Implement purchase processing
            throw new NotImplementedException($"Transaction type PURCHASE not yet implemented for transaction {transaction.TransactionId}");
        }
        #endregion
    }
}
