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

            // Group transactions by status
            var transactionsByStatus = transactions
                .GroupBy(t => t.TransactionStatus?.ToString() ?? "NULL")
                .OrderBy(g => g.Key);

            Console.WriteLine("\n=== Transaction Status Distribution ===");
            foreach (var group in transactionsByStatus)
            {
                Console.WriteLine($"  {group.Key}: {group.Count()}");
            }

            // Process each transaction based on its status
            foreach (var transaction in transactions)
            {
                try
                {
                    var transactionEntries = ProcessTransactionByStatus(transaction, orders, feeBayUserName);
                    result.AddRange(transactionEntries);
                }
                catch (NotImplementedException ex)
                {
                    Console.WriteLine($"Skipping transaction {transaction.TransactionId}: {ex.Message}");
                    validationErrors.Add($"Transaction {transaction.TransactionId}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing transaction {transaction.TransactionId}: {ex.Message}");
                    validationErrors.Add($"Transaction {transaction.TransactionId}: {ex.Message}");
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
        private List<ToGnuCash> ProcessTransactionByStatus(
            Transaction transaction,
            List<Order> orders,
            string feeBayUserName)
        {
            if (!transaction.TransactionStatus.HasValue)
            {
                throw new NotImplementedException($"Transaction {transaction.TransactionId} has null TransactionStatus");
            }

            return transaction.TransactionStatus.Value switch
            {
                TransactionStatusEnum.PAYOUT => ProcessPayoutTransaction(transaction, orders, feeBayUserName),
                TransactionStatusEnum.FUNDS_ON_HOLD => throw new NotImplementedException($"Transaction status FUNDS_ON_HOLD not yet implemented for transaction {transaction.TransactionId}"),
                TransactionStatusEnum.FUNDS_PROCESSING => throw new NotImplementedException($"Transaction status FUNDS_PROCESSING not yet implemented for transaction {transaction.TransactionId}"),
                TransactionStatusEnum.FUNDS_AVAILABLE_FOR_PAYOUT => throw new NotImplementedException($"Transaction status FUNDS_AVAILABLE_FOR_PAYOUT not yet implemented for transaction {transaction.TransactionId}"),
                TransactionStatusEnum.COMPLETED => throw new NotImplementedException($"Transaction status COMPLETED not yet implemented for transaction {transaction.TransactionId}"),
                TransactionStatusEnum.FAILED => throw new NotImplementedException($"Transaction status FAILED not yet implemented for transaction {transaction.TransactionId}"),
                _ => throw new NotImplementedException($"Unknown transaction status: {transaction.TransactionStatus}")
            };
        }

        private List<ToGnuCash> ProcessPayoutTransaction(
            Transaction transaction,
            List<Order> orders,
            string feeBayUserName)
        {
            // Split PAYOUT transactions by type
            // SALE and REFUND both need order/lineitem processing
            if (transaction.TransactionType == TransactionTypeEnum.SALE || 
                transaction.TransactionType == TransactionTypeEnum.REFUND)
            {
                return ProcessPayoutSaleTransaction(transaction, orders, feeBayUserName);
            }
            else
            {
                return ProcessPayoutNonSaleTransaction(transaction, feeBayUserName);
            }
        }

        private List<ToGnuCash> ProcessPayoutSaleTransaction(
            Transaction transaction,
            List<Order> orders,
            string feeBayUserName)
        {
            var result = new List<ToGnuCash>();

            // Find the matching order
            var order = orders.FirstOrDefault(o => o.OrderId == transaction.OrderId);
            if (order == null)
            {
                Console.WriteLine($"Warning: Order {transaction.OrderId} not found for PAYOUT SALE transaction {transaction.TransactionId}");
                return result;
            }

            // Build line item fee map for this transaction
            var lineItemFeeMap = BuildLineItemFeeMap(new List<Transaction> { transaction });

            foreach (var lineItem in order.LineItems)
            {
                if (!lineItemFeeMap.ContainsKey(lineItem.LineItemId))
                {
                    Console.WriteLine($"Warning: LineItem {lineItem.LineItemId} not found in transaction fee map");
                    continue;
                }

                var financeLineItem = lineItemFeeMap[lineItem.LineItemId];
                var lineItemEntries = ProcessTransaction(transaction, feeBayUserName, order, lineItem, financeLineItem);
                result.AddRange(lineItemEntries);
            }

            return result;
        }

        private List<ToGnuCash> ProcessPayoutNonSaleTransaction(
            Transaction transaction,
            string feeBayUserName)
        {
            Console.WriteLine($"Processing {transaction.TransactionType} PAYOUT transaction {transaction.TransactionId}");
            return ProcessTransaction(transaction, feeBayUserName);
        }

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
            var transactionId = $"{order.OrderId}";

            // (1) Product Sale Income
            var saleAmount = lineItem.LineItemCost?.DollarAmount() ?? 0;
            entries.Add(new ToGnuCash
            {
                Date = orderDate,
                Account = $"Income:{userMapping.AssetAccount} Sales:Product Sale",
                Description = $"eBay Order #{order.OrderId} SKU: {lineItem.SKU} - Title: {lineItem.Title}",
                Amount = saleAmount,
                TransactionId = transactionId,
                SortOrder = 1
            });

            // (2) Shipping Income
            var shippingAmount = lineItem.DeliveryCost?.ShippingCost?.DollarAmount() ?? 0;

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
            var netAmount = transaction.Amount?.DollarAmount() ?? 0;
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
                var stampCost = _localDbConnectionManager.GetStampCOGS(skuInt);
                // TEMPORARY: Using 50% fallback until database method is available

                if (stampCost == null || stampCost == 0.0m)
                {
                    Console.WriteLine($"Warning: No cost found in database for SKU {skuInt}, using 50% fallback");
                    return sellingPrice * 0.5m;
                }
                return (decimal)stampCost;



                Console.WriteLine($"Warning: No cost found in database for SKU {skuInt}, using 50% fallback");
               
                return sellingPrice * 0.5m;

                // Original implementation (commented out until StampDBService is available):
               // var db = new StampDBService.StampDBConnection();
                // var stampCost = db.GetStampCostById(skuInt);
              
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating COGS for SKU '{sku}': {ex.Message}, using 50% fallback");
                return sellingPrice * 0.5m;
            }
        }

        private List<ToGnuCash> ProcessTransaction(
            Transaction transaction,
            string feeBayUserName,
            Order order = null,
            LineItem lineItem = null,
            OrderLineItem financeLineItem = null)
        {
            if (!transaction.TransactionType.HasValue)
            {
                throw new NotImplementedException($"Transaction {transaction.TransactionId} has null TransactionType");
            }

            return transaction.TransactionType.Value switch
            {
                TransactionTypeEnum.SALE => ProcessSaleLineItem(lineItem, order, financeLineItem, transaction, feeBayUserName),
                TransactionTypeEnum.REFUND => ProcessRefund(lineItem, order, financeLineItem, transaction, feeBayUserName),
                TransactionTypeEnum.CREDIT => ProcessCredit(transaction, feeBayUserName),
                TransactionTypeEnum.DISPUTE => ProcessDispute(transaction, feeBayUserName),
                TransactionTypeEnum.SHIPPING_LABEL => ProcessShippingLabel(transaction, feeBayUserName),
                TransactionTypeEnum.TRANSFER => ProcessTransfer(transaction, feeBayUserName),
                TransactionTypeEnum.NON_SALE_CHARGE => ProcessNonSaleCharge(transaction, feeBayUserName),
                TransactionTypeEnum.ADJUSTMENT => ProcessAdjustment(transaction, feeBayUserName),
                TransactionTypeEnum.WITHDRAWAL => ProcessWithdrawal(transaction, feeBayUserName),
                TransactionTypeEnum.LOAN_REPAYMENT => ProcessLoanRepayment(transaction, feeBayUserName),
                TransactionTypeEnum.PURCHASE => ProcessPurchase(transaction, feeBayUserName),
                _ => throw new NotImplementedException($"Unknown transaction type: {transaction.TransactionType}")
            };
        }
        #endregion

        #region Transaction Type Handlers (Step 7)
     private List<ToGnuCash> ProcessRefund(
            LineItem lineItem,
            Order order,
            OrderLineItem financeLineItem,
            Transaction transaction,
            string feeBayUserName)
        {
            var entries = new List<ToGnuCash>();
            var userMapping = FeeBayUserNameMap[feeBayUserName];
            var refundDate = DateTime.Parse(transaction.TransactionDate);
            var transactionId = $"{order.OrderId}-{lineItem.LineItemId}";

            // (1) Product Sale Income - REVERSED
            var saleAmount = lineItem.LineItemCost?.DollarAmount() ?? 0;
            entries.Add(new ToGnuCash
            {
                Date = refundDate,
                Account = $"Income:{userMapping.AssetAccount} Sales:Product Sale",
                Description = $"REFUND - eBay Order #{order.OrderId}-{lineItem.LineItemId} - {lineItem.SKU} - {lineItem.Title}",
                Amount = -saleAmount,
                TransactionId = transactionId,
                SortOrder = 1
            });

            // (2) Shipping Income - REVERSED
            var shippingAmount = lineItem.DeliveryCost?.ShippingCost?.DollarAmount() ?? 0;

            if (shippingAmount > 0)
            {
                entries.Add(new ToGnuCash
                {
                    Date = refundDate,
                    Account = $"Income:{userMapping.AssetAccount} Sales:Shipping",
                    Description = string.Empty,
                    Amount = -shippingAmount,
                    TransactionId = transactionId,
                    SortOrder = 2
                });
            }

            // (3) Fixed Fee Expense - REVERSED
            var fixedFee = financeLineItem.GetFeeByType(FeeTypeEnum.FINAL_VALUE_FEE_FIXED_PER_ORDER) ?? 0;
            if (fixedFee > 0)
            {
                entries.Add(new ToGnuCash
                {
                    Date = refundDate,
                    Account = $"Expenses:eBay Fees:{userMapping.FeeAccount}:Fixed Fee Per Sale",
                    Description = string.Empty,
                    Amount = -fixedFee,
                    TransactionId = transactionId,
                    SortOrder = 3
                });
            }

            // (4) Final Value Fee Expense - REVERSED
            var finalValueFee = financeLineItem.GetFeeByType(FeeTypeEnum.FINAL_VALUE_FEE) ?? 0;
            var finalValueShippingFee = financeLineItem.GetFeeByType(FeeTypeEnum.FINAL_VALUE_SHIPPING_FEE) ?? 0;
            var totalFinalValueFee = finalValueFee + finalValueShippingFee;

            if (totalFinalValueFee > 0)
            {
                entries.Add(new ToGnuCash
                {
                    Date = refundDate,
                    Account = $"Expenses:eBay Fees:{userMapping.FeeAccount}:Final Value Fees",
                    Description = string.Empty,
                    Amount = -totalFinalValueFee,
                    TransactionId = transactionId,
                    SortOrder = 4
                });
            }

            // (5) International Fee Expense - REVERSED
            var internationalFee = financeLineItem.GetFeeByType(FeeTypeEnum.INTERNATIONAL_FEE) ?? 0;
            if (internationalFee > 0)
            {
                entries.Add(new ToGnuCash
                {
                    Date = refundDate,
                    Account = $"Expenses:eBay Fees:{userMapping.FeeAccount}:International Fee",
                    Description = string.Empty,
                    Amount = -internationalFee,
                    TransactionId = transactionId,
                    SortOrder = 5
                });
            }

            // (6) eBay Asset line - REVERSED (positive for refund since money is leaving eBay balance)
            var netAmount = transaction.Amount?.DollarAmount() ?? 0;
            var orderLineItemCount = order.LineItems?.Count ?? 1;
            var proportionalNet = netAmount / orderLineItemCount;

            entries.Add(new ToGnuCash
            {
                Date = refundDate,
                Account = $"Assets:Current Assets:eBay:{userMapping.AssetAccount}",
                Description = string.Empty,
                Amount = proportionalNet,
                TransactionId = transactionId,
                SortOrder = 6
            });

            // (7) COGS Expense - REVERSED
            var cogs = CalculateCOGS(lineItem.SKU, saleAmount);
            entries.Add(new ToGnuCash
            {
                Date = refundDate,
                Account = "Expenses:Cost of Goods Sold",
                Description = $"COGS reversal for SKU {lineItem.SKU}",
                Amount = -cogs,
                TransactionId = transactionId,
                SortOrder = 7
            });

            // (8) Inventory Asset - REVERSED (inventory comes back)
            entries.Add(new ToGnuCash
            {
                Date = refundDate,
                Account = "Assets:INVENTORY",
                Description = $"COGS reversal for SKU {lineItem.SKU}",
                Amount = cogs,
                TransactionId = transactionId,
                SortOrder = 8
            });

            return entries;
        }
     

        private List<ToGnuCash> ProcessCredit(Transaction transaction, string feeBayUserName)
        {
            // TODO: Implement credit processing
            throw new NotImplementedException($"Transaction type CREDIT not yet implemented for transaction {transaction.TransactionId}");
        }

        private List<ToGnuCash> ProcessDispute(Transaction transaction, string feeBayUserName)
        {
            // TODO: Implement dispute processing
            throw new NotImplementedException($"Transaction type DISPUTE not yet implemented for transaction {transaction.TransactionId}");
        }

        private List<ToGnuCash> ProcessShippingLabel(Transaction transaction, string feeBayUserName)
        {
             var entries = new List<ToGnuCash>();
            var userMapping = FeeBayUserNameMap[feeBayUserName];
              entries.Add(new ToGnuCash
            {
                Date = DateTime.Parse(transaction.TransactionDate)   ,          
                Account = $"Assets:Current Assets:eBay:{userMapping.AssetAccount}",
                Description = $"{transaction.OrderId} - {transaction.TransactionMemo}",
                Amount = (decimal)(transaction.Amount?.DollarAmount() ?? 0),
                TransactionId = transaction.TransactionId,
                SortOrder = 1
            });
            // TODO: Implement shipping label processing
                  entries.Add(new ToGnuCash
            {
                Date = DateTime.Parse(transaction.TransactionDate)   ,          
                Account = $"",
                Description = $"{transaction.OrderId} - {transaction.TransactionMemo}",
                Amount = (decimal)(transaction.Amount?.DollarAmount() ?? 0),
                TransactionId = transaction.TransactionId,
                SortOrder = 2
            });
            return entries;
       }

        private List<ToGnuCash> ProcessTransfer(Transaction transaction, string feeBayUserName)
        {
            // TODO: Implement transfer processing
            throw new NotImplementedException($"Transaction type TRANSFER not yet implemented for transaction {transaction.TransactionId}");
        }

        private List<ToGnuCash> ProcessNonSaleCharge(Transaction transaction, string feeBayUserName)
        {
            // TODO: Implement non-sale charge processing THIS IS STORE SUBSCRIPTION FEE 
             var entries = new List<ToGnuCash>();
             FeeCategoryEnum feeCategoryEnum = Classify(transaction);
            switch (feeCategoryEnum)
            {
                case FeeCategoryEnum.StoreSubscription:
                   
                    var userMapping = FeeBayUserNameMap[feeBayUserName];
                    entries.Add(new ToGnuCash
                    {
                        Date = DateTime.Parse(transaction.TransactionDate),
                        Account = $"Expenses:eBay Fees:{userMapping.FeeAccount}:Store Monthly Fee",  
                        Description = $"{transaction.TransactionMemo}",
                        Amount = (decimal)(transaction.Amount?.DollarAmount() ?? 0),
                        TransactionId = transaction.TransactionId,
                        SortOrder = 1
                    });
                        entries.Add(new ToGnuCash
                        {
                            Date = DateTime.Parse(transaction.TransactionDate),
                            Account = $"Assets:Current Assets:eBay:{userMapping.AssetAccount}",
                            Description = $"{transaction.TransactionMemo}",
                            Amount = -(decimal)(transaction.Amount?.DollarAmount() ?? 0),
                            TransactionId = transaction.TransactionId,
                            SortOrder = 2
                        });
                        break;
                  


    //var otherFeeLineFrom = new Stripe.StripeModels.OutputData();
                   // otherFeeLineFrom.Date = DateOnly.FromDateTime(DateTime.Parse(otherFee.Transaction_creation_date));
                    //otherFeeLineFrom.Account = $"Assets:Current Assets:feeBay:{feeBayName2}";
                    //otherFeeLineFrom.Description = otherFee.Description;
                    //otherFeeLineFrom.Amount = -decimal.Parse(otherFee.Net_amount);
               //     otherFeeLineFrom.TransactionId = otherFee.Reference_ID;
                //    otherFeeLineFrom.SortOrder = 1;
                  //  outputData.Add(otherFeeLineFrom);

              //      var otherFeeLineTo = new Stripe.StripeModels.OutputData();
                //    otherFeeLineTo.Date = DateOnly.FromDateTime(DateTime.Parse(otherFee.Transaction_creation_date));
                  //  otherFeeLineTo.Account = $"Expenses:FeeBay Fees:{feeBayName1}:Store Monthly Fee";
                    //otherFeeLineTo.Description = string.Empty; //  payout.Description;
               //     otherFeeLineTo.Amount = decimal.Parse(otherFee.Net_amount);
                 //   otherFeeLineTo.TransactionId = otherFee.Reference_ID;
                   // otherFeeLineTo.SortOrder = 2;
                 //   outputData.Add(otherFeeLineTo);





               case FeeCategoryEnum.Advertising :
                    throw new NotImplementedException($"Transaction type AD_FEE not yet implemented for transaction {transaction.TransactionId}");
            
                case FeeCategoryEnum.SellingFees :
                    throw new NotImplementedException($"Transaction type SELLING_FEE not yet implemented for transaction {transaction.TransactionId}");
                    
                case FeeCategoryEnum.OtherEbayFees :
                    throw new NotImplementedException($"Transaction type OTHER_EBAY_FEES not yet implemented for transaction {transaction.TransactionId}");

                default:
                    throw new NotImplementedException($"Unknown fee category for transaction {transaction.TransactionId}");
            }
              return entries;
        }
private FeeCategoryEnum Classify(Transaction txn)
{
    if (txn.FeeType == FeeTypeEnum.EBAY_STORE_SUBSCRIPTION_FEE)
        return FeeCategoryEnum.StoreSubscription;

    if (txn.FeeType == FeeTypeEnum.AD_FEE)
        return FeeCategoryEnum.Advertising;

    if (txn.FeeType == FeeTypeEnum.FINAL_VALUE_FEE)
        return FeeCategoryEnum.SellingFees;

    // Fallbacks for eBay oddities
    if (!string.IsNullOrEmpty(txn.TransactionMemo))
    {
        if (txn.TransactionMemo.Contains("Store", StringComparison.OrdinalIgnoreCase) &&
            txn.TransactionMemo.Contains("subscription", StringComparison.OrdinalIgnoreCase))
            return FeeCategoryEnum.StoreSubscription;
    }
    var regex = new System.Text.RegularExpressions.Regex(
    @"^(\d{4}-\d{2}-\d{2})\s*-\s*(\d{4}-\d{2}-\d{2})$");

    var match = regex.Match(txn.TransactionMemo);

    if (match.Success)
    {
    var startDate = DateOnly.Parse(match.Groups[1].Value);
    var endDate = DateOnly.Parse(match.Groups[2].Value);

    // Looks like a subscription billing period
    return FeeCategoryEnum.StoreSubscription;
    }

    return FeeCategoryEnum.OtherEbayFees;
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

    internal enum FeeCategoryEnum
    {
        StoreSubscription,
        Advertising,
        SellingFees,
        OtherEbayFees
    }
}
