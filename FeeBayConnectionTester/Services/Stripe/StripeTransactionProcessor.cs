using FeeBayConnectionTester.DTO;
using FeeBayConnectionTester.Extensions;
using LocalDBConnections;
using LocalDBConnections.StampDataDB.StampdataEntities;
using Stripe;
using System.Text.RegularExpressions;

namespace FeeBayConnectionTester.Services.Stripe
{
    public class StripeTransactionProcessor : IStripeTransactionProcessor
    {
        #region Constants and Fields
        private readonly ILocalDbConnectionManager _localDbConnectionManager;
        #endregion

        #region Constructors
        public StripeTransactionProcessor(ILocalDbConnectionManager localDbConnectionManager)
        { _localDbConnectionManager = localDbConnectionManager; }
        #endregion

        #region Methods
        #region Public Methods
        public async Task<List<ToGnuCash>> ReformatStripeForGnuCashAsync(List<BalanceTransaction> incomingRecords)
        {
            var groupByTransactionId = from record in incomingRecords
                group record by record.Id into newGroup select newGroup;

            var cleanedRecords = new List<ToGnuCash>();
            foreach(var fullTransaction in groupByTransactionId)
            {
                IList<ToGnuCash> oneTransaction = new List<ToGnuCash>();
                foreach (var record in fullTransaction)
                {
                    ToGnuCash stripeSalesRecord;// = new();
                    ToGnuCash stripeFeeRecord;// = new();
                    ToGnuCash stripePayoutFromRecord;// = new();
                    ToGnuCash stripePayoutToRecord;// = new();
                    ToGnuCash stripeRefundRecord;// = new();
                    ToGnuCash stripeCOGSRecord;// = new();
                    ToGnuCash stripeInventoryRecord;// = new();
                    ToGnuCash stripeIncomingCashRecord;// = new();

                    if (!record.Description.Contains("5046"))
                    {
                       // continue;
                    }


                    switch (record.ReportingCategory)
                    {
                        case "refund":
                            //! This treats a refund as the exact opposite of a sale
                            //! May not be the correct way to do this

                            // subtract from Income
                            stripeRefundRecord = new();
                            stripeRefundRecord.Date = DateOnly.FromDateTime(record.AvailableOn);
                            stripeRefundRecord.Account = "Income:DSD Website Sales:Stripe CC Sale";
                            stripeRefundRecord.Description = $"NopCommerce {record.Description}";
                            stripeRefundRecord.Amount = record.Amount.Cents2Dollars();
                            stripeRefundRecord.TransactionId = record.Id;
                            stripeRefundRecord.SortOrder = 1;
                            oneTransaction.Add(stripeRefundRecord);

                            // subtract from COGS
                            stripeCOGSRecord = new();
                            stripeCOGSRecord.Date = DateOnly.FromDateTime(record.AvailableOn);
                            stripeCOGSRecord.Account = "Expenses:Cost of Goods Sold";
                            stripeCOGSRecord.Description = string.Empty;//$"NopCommerce {record.Description}";// - Order GUID {record.OrderGuid}";
                            stripeCOGSRecord.Amount = -(await MakeCogsForFullOrder(record));//Decimal.Parse(record.gross) / 2;
                            stripeCOGSRecord.TransactionId = record.Id;
                            stripeCOGSRecord.SortOrder = 2;
                            oneTransaction.Add(stripeCOGSRecord);

                            //put back into Inventory
                            stripeInventoryRecord = new();
                            stripeInventoryRecord.Date = DateOnly.FromDateTime(record.AvailableOn);
                            stripeInventoryRecord.Account = "Assets:INVENTORY";
                            stripeInventoryRecord.Description = string.Empty;//$"NopCommerce Order #{record.Description}";
                            stripeInventoryRecord.Amount = await MakeCogsForFullOrder(record);// Decimal.Parse(record.gross) / 2;
                            stripeInventoryRecord.TransactionId = record.Id;
                            stripeInventoryRecord.SortOrder = 3;
                            oneTransaction.Add(stripeInventoryRecord);

                            // subtract original  //!Fees
                            stripeFeeRecord = new();
                            stripeFeeRecord.Date = DateOnly.FromDateTime(record.AvailableOn);
                            stripeFeeRecord.Account = "Expenses:StripeCC Fees";
                            stripeFeeRecord.Description = string.Empty;// $"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid} Stripe Fee";
                            stripeFeeRecord.Amount = record.Fee.Cents2Dollars();
                            stripeFeeRecord.TransactionId = record.Id;
                            stripeFeeRecord.SortOrder = 4;
                            oneTransaction.Add(stripeFeeRecord);

                            break;

                        case "charge":
                            var orderId = Regex.Matches(record.Description, @"\d+").FirstOrDefault()?.Value ??
                                string.Empty;
                            var orderLineItems = await PullOrderLineItems(orderId);
                            bool totalsMatch = CheckOrderTotals(orderLineItems, record);
                            if (!totalsMatch)
                            {
                                //MessageBox.Show($"Order {orderId} Totals don't match!");
                                // Add purchase to Income
                                stripeSalesRecord = new();
                                stripeSalesRecord.Date = DateOnly.FromDateTime(record.Created);
                                stripeSalesRecord.Account = "Income:DSD Website Sales:Stripe CC Sale";
                                stripeSalesRecord.Description = $"NopCommerce {record.Description} Line Items not available";
                                stripeSalesRecord.Amount = record.Amount.Cents2Dollars();
                                stripeSalesRecord.TransactionId = record.Id;
                                stripeSalesRecord.SortOrder = 1;
                                oneTransaction.Add(stripeSalesRecord);

                                // Add purchases to //!COGS
                                stripeCOGSRecord = new();
                                stripeCOGSRecord.Date = DateOnly.FromDateTime(record.Created);
                                stripeCOGSRecord.Account = "Expenses:Cost of Goods Sold";
                                stripeCOGSRecord.Description = string.Empty;//mmerce {record.Description}";// - Order GUID {record.OrderGuid}";
                                stripeCOGSRecord.Amount = -(await MakeCogsForFullOrder(record));//Decimal.Parse(record.gross) / 2;
                                stripeCOGSRecord.TransactionId = record.Id;
                                stripeCOGSRecord.SortOrder = 2;
                                oneTransaction.Add(stripeCOGSRecord);

                                // now subtract from //!inventory
                                stripeInventoryRecord = new();
                                stripeInventoryRecord.Date = DateOnly.FromDateTime(record.Created);
                                stripeInventoryRecord.Account = "Assets:INVENTORY";
                                stripeInventoryRecord.Description = string.Empty;//$"NopCommerce Order #{record.Description}";
                                stripeInventoryRecord.Amount = await MakeCogsForFullOrder(record);// Decimal.Parse(record.gross) / 2;
                                stripeInventoryRecord.TransactionId = record.Id;
                                stripeInventoryRecord.SortOrder = 3;
                                oneTransaction.Add(stripeInventoryRecord);

                                // and add Stripe Processing Fees to Expences
                                stripeFeeRecord = new();
                                stripeFeeRecord.Date = DateOnly.FromDateTime(record.Created);
                                stripeFeeRecord.Account = "Expenses:StripeCC Fees";
                                stripeFeeRecord.Description = string.Empty;// $"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid} Stripe Fee";
                                stripeFeeRecord.Amount = -record.Fee.Cents2Dollars();
                                stripeFeeRecord.TransactionId = record.Id;
                                stripeFeeRecord.SortOrder = 4;
                                oneTransaction.Add(stripeFeeRecord);

                                // and put net income into the holding account for payout to  checking
                                stripeIncomingCashRecord = new();
                                stripeIncomingCashRecord.Date = DateOnly.FromDateTime(record.Created);
                                stripeIncomingCashRecord.Account = "Assets:Incoming Cash:website";
                                stripeIncomingCashRecord.Description = string.Empty;//$"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid} XFER to Checking";
                                stripeIncomingCashRecord.Amount = -record.Net.Cents2Dollars();
                                stripeIncomingCashRecord.TransactionId = record.Id;
                                stripeIncomingCashRecord.SortOrder = 5;
                                oneTransaction.Add(stripeIncomingCashRecord);
                                break;
                            }
                            bool isFirstLineItem = true;
                            foreach (var oli in orderLineItems)
                            {
                                // decimal proRatedAmount = ProRateAmount(oli, record);
                                // Add purchase to Income
                                stripeSalesRecord = new();
                                stripeSalesRecord.Date = DateOnly.FromDateTime(record.Created);
                                stripeSalesRecord.Account = "Income:DSD Website Sales:Stripe CC Sale";
                                stripeSalesRecord.Description = $"NopCommerce {record.Description} SKU {oli.SKU}";
                                stripeSalesRecord.Amount = oli.Sales_Price;//ProRateAmount(oli, record);
                                stripeSalesRecord.TransactionId = record.Id+"-"+oli.SKU;
                                stripeSalesRecord.SortOrder = 1;
                                oneTransaction.Add(stripeSalesRecord);

                                // Add purchases to //!COGS
                                stripeCOGSRecord = new();
                                stripeCOGSRecord.Date = DateOnly.FromDateTime(record.Created);
                                stripeCOGSRecord.Account = "Expenses:Cost of Goods Sold";
                                stripeCOGSRecord.Description = string.Empty;//mmerce {record.Description}";// - Order GUID {record.OrderGuid}";
                                stripeCOGSRecord.Amount = -MakeCogsForLineItem(oli);// (await MakeCogsForFullOrder(record));//Decimal.Parse(record.gross) / 2;
                                stripeCOGSRecord.TransactionId = record.Id+"-"+oli.SKU;
                                stripeCOGSRecord.SortOrder = 2;
                                oneTransaction.Add(stripeCOGSRecord);

                                // now subtract from //!inventory
                                stripeInventoryRecord = new();
                                stripeInventoryRecord.Date = DateOnly.FromDateTime(record.Created);
                                stripeInventoryRecord.Account = "Assets:INVENTORY";
                                stripeInventoryRecord.Description = string.Empty;//$"NopCommerce Order #{record.Description}";
                                stripeInventoryRecord.Amount = MakeCogsForLineItem(oli);// -stripeCOGSRecord.Amount;// Decimal.Parse(record.gross) / 2;
                                stripeInventoryRecord.TransactionId = record.Id + "-" + oli.SKU;
                                stripeInventoryRecord.SortOrder = 3;
                                oneTransaction.Add(stripeInventoryRecord);

                               
                                // and add Stripe Processing Fees to Expences
                                stripeFeeRecord = new();
                                stripeFeeRecord.Date = DateOnly.FromDateTime(record.Created);
                                stripeFeeRecord.Account = "Expenses:StripeCC Fees";
                                stripeFeeRecord.Description = string.Empty;
                                // $"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid} Stripe Fee";
                                if(isFirstLineItem)
                                {
                                    stripeFeeRecord.Amount = -record.Fee.Cents2Dollars();
                                }
                                else
                                {
                                    stripeFeeRecord.Amount = 0.0m;
                                }
                                stripeFeeRecord.TransactionId = record.Id + "-" + oli.SKU;
                                stripeFeeRecord.SortOrder = 4;
                                oneTransaction.Add(stripeFeeRecord);
                                
                                // and put net income into the holding account for payout to  checking
                                stripeIncomingCashRecord = new();
                                stripeIncomingCashRecord.Date = DateOnly.FromDateTime(record.Created);
                                stripeIncomingCashRecord.Account = "Assets:Incoming Cash:website";
                                stripeIncomingCashRecord.Description = string.Empty;//$"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid} XFER to Checking";
                                if(isFirstLineItem)
                                {
                                    stripeIncomingCashRecord.Amount = -(oli.Sales_Price - record.Fee.Cents2Dollars());//ProRateFee(oli,record));// -record.Net.Cents2Dollars();
                                }
                                else
                                {
                                    stripeIncomingCashRecord.Amount = -oli.Sales_Price;
                                }
                                stripeIncomingCashRecord.TransactionId = record.Id + "-" + oli.SKU;
                                stripeIncomingCashRecord.SortOrder = 5;
                                oneTransaction.Add(stripeIncomingCashRecord);
                                isFirstLineItem = false;
                            }
                            break;
                        case "payout":
                            //Subtract from the holding account
                            stripePayoutFromRecord = new();
                            stripePayoutFromRecord.Date = DateOnly.FromDateTime(record.AvailableOn);
                            stripePayoutFromRecord.Account = "Assets:Incoming Cash:website";
                            stripePayoutFromRecord.Description = $"NopCommerce {record.Description} Payout";
                            stripePayoutFromRecord.Amount = -record.Amount.Cents2Dollars();
                            stripePayoutFromRecord.TransactionId = record.Id;
                            stripePayoutFromRecord.SortOrder = 1;
                            oneTransaction.Add(stripePayoutFromRecord);

                            stripePayoutToRecord = new();
                            stripePayoutToRecord.Date = DateOnly.FromDateTime(record.AvailableOn);
                            stripePayoutToRecord.Account = "Assets:Current Assets:TCCU Business Checking";
                            stripePayoutToRecord.Description = string.Empty;//$"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid} XFER to Checking";
                            stripePayoutToRecord.Amount = record.Amount.Cents2Dollars();
                            stripePayoutToRecord.TransactionId = record.Id;
                            stripePayoutToRecord.SortOrder = 2;
                            oneTransaction.Add(stripePayoutToRecord);
                            break;
                        default:
                            break;
                    }

                    cleanedRecords.AddRange(oneTransaction.OrderBy(r => r.TransactionId)
                                                            .ThenBy(r => r.SortOrder));
                    //cleanedRecords.AddRange(from o in oneTransaction orderby o.SortOrder select o);
                }
            }
            return cleanedRecords;
        }
        #endregion

        #region Private Methods
        private bool CheckOrderTotals(List<Order_Line_Items_By_Order_Id> orderLineItems, BalanceTransaction record)
        {
            if(orderLineItems == null || orderLineItems.Count == 0)
            {
                return false;
            }

            var orderLineItemsTotalSale = orderLineItems.Sum(x => x.Sales_Price);// ?? 0m;
            return orderLineItemsTotalSale == record.Amount.Cents2Dollars();
        }

        private async Task<decimal> MakeCogsForFullOrder(BalanceTransaction record)
        {
            if(record.Description.Contains("5060"))
            {
                // return 0.0m;
            }
            var sellingPrice = record.Amount.Cents2Dollars();
            decimal fullOrderCogs = 0.0m;
            // if (sellingPrice == 130.0m)
            // {
            //     return fullOrderCogs;
            // }
            var orderId = Regex.Matches(record.Description, @"\d+").FirstOrDefault()?.Value ?? string.Empty;
            if(orderId == "5044")
            {
                // return 0m;
            }
            // var skus = await PullOutSkus(orderId); // Fixed: Create an instance of StripeCleaner to call the non-static method
            var orderLineItems = await PullOrderLineItems(orderId);
            foreach(var oli in orderLineItems)
            {
                // Extract the numeric part of the SKU
                var singleStampCogs = oli.Cost;//_localDbConnectionManager.GetStampCOGS(oli);
                if(singleStampCogs == 0.0m)
                {
                    singleStampCogs = (oli.Sales_Price / 2);
                } else
                {
                    singleStampCogs = singleStampCogs;
                }
                fullOrderCogs += singleStampCogs;
            }
            return fullOrderCogs;
        }

        private decimal MakeCogsForLineItem(Order_Line_Items_By_Order_Id oli)
        {
            if(oli.Cost == 0)
            {
                oli.Cost = oli.Sales_Price / 2;
            }
            return oli.Cost;
        }

        private decimal ProRateAmount(Order_Line_Items_By_Order_Id oli, BalanceTransaction record)
        {
            decimal lineItemSalesPrice = oli.Sales_Price;
            decimal totalSale = record.Amount.Cents2Dollars();
            return lineItemSalesPrice / totalSale;
        }

        private decimal ProRateFee(Order_Line_Items_By_Order_Id oli, BalanceTransaction record)
        {
            decimal totalFee = record.Fee;
            decimal totalSale = record.Amount;
            decimal lineItemPrice = oli.Sales_Price;
            decimal prf = totalFee * (lineItemPrice / totalSale);
            return prf;
        }

        private async Task<List<Order_Line_Items_By_Order_Id>> PullOrderLineItems(string nopOrderId)
        { return await _localDbConnectionManager.GetSoldNopStampsViewAsync(nopOrderId); }
        #endregion
        #endregion
    }
}
