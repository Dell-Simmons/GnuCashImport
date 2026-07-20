using System.Text.RegularExpressions;
using FeeBayConnectionTester.DTO;
using LocalDBConnections;
using Stripe;
using FeeBayConnectionTester.Extensions;
using LocalDBConnections.StampDataDB.StampdataEntities;
namespace FeeBayConnectionTester.Services.Stripe
{
    public class StripeTransactionProcessor : IStripeTransactionProcessor
    {
        #region Constants and Fields
        private readonly ILocalDbConnectionManager _localDbConnectionManager;
        #endregion
        public StripeTransactionProcessor(ILocalDbConnectionManager localDbConnectionManager)
        {
            _localDbConnectionManager = localDbConnectionManager;
        }
        #region Methods
        public async Task<List<ToGnuCash>> ReformatStripeForGnuCashAsync(List<BalanceTransaction> incomingRecords)
        {
            var groupByTransactionId = from record in incomingRecords
                                       group record by record.Id into newGroup
                                       select newGroup;

            var cleanedRecords = new List<ToGnuCash>();
            foreach (var fullTransaction in groupByTransactionId)
            {
                IList<ToGnuCash> oneTransaction = new List<ToGnuCash>();
                foreach (var record in fullTransaction)
                {
                    ToGnuCash stripeSalesRecord = new();
                    ToGnuCash stripeFeeRecord = new();
                    ToGnuCash stripePayoutFromRecord = new();
                    ToGnuCash stripePayoutToRecord = new();
                    ToGnuCash stripeRefundRecord = new();
                    ToGnuCash stripeCOGSRecord = new();
                    ToGnuCash stripeInventoryRecord = new();
                    ToGnuCash stripeIncomingCashRecord = new();
                    if (!record.Description.Contains("5039"))
                            {
                          //      continue;
                            }

                    var orderId = Regex.Matches(record.Description, @"\d+").FirstOrDefault()?.Value ?? string.Empty;
                    var orderLineItems = await PullOrderLineItems(orderId);
                    foreach (var oli in orderLineItems)
                    {
                        switch (record.ReportingCategory)
                        {
                            case "refund":
                                //! This treats a refund as the exact opposite of a sale
                                //! May not be the correct way to do this

                                // subtract from Income
                                stripeRefundRecord.Date = DateOnly.FromDateTime(record.AvailableOn);
                                stripeRefundRecord.Account = "Income:DSD Website Sales:Stripe CC Sale";
                                stripeRefundRecord.Description = $"NopCommerce {record.Description}";
                                stripeRefundRecord.Amount = record.Amount.Cents2Dollars();
                                stripeRefundRecord.TransactionId = record.Id;
                                stripeRefundRecord.SortOrder = 1;
                                oneTransaction.Add(stripeRefundRecord);

                                // subtract from COGS
                                stripeCOGSRecord.Date = DateOnly.FromDateTime(record.AvailableOn);
                                stripeCOGSRecord.Account = "Expenses:Cost of Goods Sold";
                                stripeCOGSRecord.Description = string.Empty;//$"NopCommerce {record.Description}";// - Order GUID {record.OrderGuid}";
                                stripeCOGSRecord.Amount = -(await MakeCogsForFullOrder(record));//Decimal.Parse(record.gross) / 2;
                                stripeCOGSRecord.TransactionId = record.Id;
                                stripeCOGSRecord.SortOrder = 2;
                                oneTransaction.Add(stripeCOGSRecord);

                                //put back into Inventory
                                stripeInventoryRecord.Date = DateOnly.FromDateTime(record.AvailableOn);
                                stripeInventoryRecord.Account = "Assets:INVENTORY";
                                stripeInventoryRecord.Description = string.Empty;//$"NopCommerce Order #{record.Description}";
                                stripeInventoryRecord.Amount = stripeCOGSRecord.Amount;// Decimal.Parse(record.gross) / 2;
                                stripeInventoryRecord.TransactionId = record.Id;
                                stripeInventoryRecord.SortOrder = 3;
                                oneTransaction.Add(stripeInventoryRecord);

                                // subtract original  //!Fees
                                stripeFeeRecord.Date = DateOnly.FromDateTime(record.AvailableOn);
                                stripeFeeRecord.Account = "Expenses:StripeCC Fees";
                                stripeFeeRecord.Description = string.Empty;// $"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid} Stripe Fee";
                                stripeFeeRecord.Amount = record.Fee.Cents2Dollars();
                                stripeFeeRecord.TransactionId = record.Id;
                                stripeFeeRecord.SortOrder = 4;
                                oneTransaction.Add(stripeFeeRecord);

                                break;

                            case "charge":

                                // Add purchase to Income
                                stripeSalesRecord.Date = DateOnly.FromDateTime(record.Created);
                                stripeSalesRecord.Account = "Income:DSD Website Sales:Stripe CC Sale";
                                stripeSalesRecord.Description = $"NopCommerce {record.Description}";
                                stripeSalesRecord.Amount = record.Amount.Cents2Dollars();
                                stripeSalesRecord.TransactionId = record.Id;
                                stripeSalesRecord.SortOrder = 1;
                                oneTransaction.Add(stripeSalesRecord);

                                // Add purchases to //!COGS
                                stripeCOGSRecord.Date = DateOnly.FromDateTime(record.Created);
                                stripeCOGSRecord.Account = "Expenses:Cost of Goods Sold";
                                stripeCOGSRecord.Description = string.Empty;//mmerce {record.Description}";// - Order GUID {record.OrderGuid}";
                                stripeCOGSRecord.Amount = -(await MakeCogsForFullOrder(record));//Decimal.Parse(record.gross) / 2;
                                stripeCOGSRecord.TransactionId = record.Id;
                                stripeCOGSRecord.SortOrder = 2;
                                oneTransaction.Add(stripeCOGSRecord);

                                // now subtract from //!inventory
                                stripeInventoryRecord.Date = DateOnly.FromDateTime(record.Created);
                                stripeInventoryRecord.Account = "Assets:INVENTORY";
                                stripeInventoryRecord.Description = string.Empty;//$"NopCommerce Order #{record.Description}";
                                                                                 // stripeCOGSRecord is already negative, so this is a positive number to add back to inventory
                                stripeInventoryRecord.Amount = -stripeCOGSRecord.Amount;// Decimal.Parse(record.gross) / 2;
                                stripeInventoryRecord.TransactionId = record.Id;
                                stripeInventoryRecord.SortOrder = 3;
                                oneTransaction.Add(stripeInventoryRecord);

                                // and add Stripe Processing Fees to Expences
                                stripeFeeRecord.Date = DateOnly.FromDateTime(record.Created);
                                stripeFeeRecord.Account = "Expenses:StripeCC Fees";
                                stripeFeeRecord.Description = string.Empty;// $"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid} Stripe Fee";
                                stripeFeeRecord.Amount = -record.Fee.Cents2Dollars();
                                stripeFeeRecord.TransactionId = record.Id;
                                stripeFeeRecord.SortOrder = 4;
                                oneTransaction.Add(stripeFeeRecord);

                                // and put net income into the holding account for payout to  checking
                                stripeIncomingCashRecord.Date = DateOnly.FromDateTime(record.Created);
                                stripeIncomingCashRecord.Account = "Assets:Incoming Cash:website";
                                stripeIncomingCashRecord.Description = string.Empty;//$"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid} XFER to Checking";
                                stripeIncomingCashRecord.Amount = -record.Net.Cents2Dollars();
                                stripeIncomingCashRecord.TransactionId = record.Id;
                                stripeIncomingCashRecord.SortOrder = 5;
                                oneTransaction.Add(stripeIncomingCashRecord);
                                break;

                            case "payout":
                                //Subtract from the holding account
                                stripePayoutFromRecord.Date = DateOnly.FromDateTime(record.AvailableOn);
                                stripePayoutFromRecord.Account = "Assets:Incoming Cash:website";
                                stripePayoutFromRecord.Description = $"NopCommerce {record.Description} Payout";
                                stripePayoutFromRecord.Amount = -record.Amount.Cents2Dollars();
                                stripePayoutFromRecord.TransactionId = record.Id;
                                stripePayoutFromRecord.SortOrder = 1;
                                oneTransaction.Add(stripePayoutFromRecord);

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
                    }
                }

                cleanedRecords.AddRange(from o in oneTransaction orderby o.SortOrder select o);
            }
            return cleanedRecords;
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
            foreach (var oli in orderLineItems)
            {
                // Extract the numeric part of the SKU
                var singleStampCogs = oli.Cost;//_localDbConnectionManager.GetStampCOGS(oli);
                if (singleStampCogs == 0.0m)
                {
                    singleStampCogs = (oli.Sales_Price / 2);
                }
                else
                {
                    singleStampCogs = (decimal)(singleStampCogs);
                }
                fullOrderCogs += (decimal)singleStampCogs;
            }
            return fullOrderCogs;
        }

        private async Task<List<string>> PullOutSkus(string NopOrderId)
        {
            if (NopOrderId == "5060")
            {
                var lookit = await _localDbConnectionManager.GetSoldNopStampsViewAsync(NopOrderId);
            }
           
            return await _localDbConnectionManager.GetSoldNopStampsAsync(NopOrderId); // Use instance field `db` directly
        }
        private async Task<List<Order_Line_Items_By_Order_Id>> PullOrderLineItems(string nopOrderId)
        {
            return await _localDbConnectionManager.GetSoldNopStampsViewAsync(nopOrderId);
        }
        #endregion
    }
}
