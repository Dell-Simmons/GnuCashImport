using FeeBayConnectionTester.DTO;
using System;
using System.Linq;

namespace GnuCashCSVImporter.Stripe
{
    public class StripeCleaner
    {
        #region Constants and Fields
        private StampDBService.StampDBConnection db = new();
        #endregion

        #region Methods
        public List<ToGnuCash> ReformatStripeForGnuCash(IEnumerable<IncomingModels.Stripe.StripeIncomingData> incomingRecords)
        {
            var groupByTransactionId = from record in incomingRecords
                group record by record.Transfer into newGroup select newGroup;

            var cleanedRecords = new List<ToGnuCash>();
            foreach(var fullTransaction in groupByTransactionId)
            {
                IList<ToGnuCash> oneTransaction = new List<ToGnuCash>();
                foreach(var record in fullTransaction)
                {
                    StripeModels.OutputData stripeSalesRecord = new();
                    StripeModels.OutputData stripeFeeRecord = new();
                    StripeModels.OutputData stripePayoutRecord = new();

                    switch(record.Type)
                    {
                        case "charge":

                            stripeSalesRecord.Date = DateOnly.FromDateTime(record.Available_On);
                            stripeSalesRecord.Account = "Income:DSD Website Sales:Stripe CC Sale";
                            stripeSalesRecord.Description = $"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid}";
                            stripeSalesRecord.Amount = record.Amount;
                            stripeSalesRecord.TransactionId = record.Transfer;
                            stripeSalesRecord.SortOrder = 1;
                            oneTransaction.Add(stripeSalesRecord);

                            // Define COGS as 1/2 of sale price
                            // And add to the cost of goods sold
                            StripeModels.OutputData stripeCOGSRecord = new();
                            stripeCOGSRecord.Date = DateOnly.FromDateTime(record.Available_On);
                            stripeCOGSRecord.Account = "Expenses:Cost of Goods Sold";
                            stripeCOGSRecord.Description = $"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid}";
                            stripeCOGSRecord.Amount = -record.Amount / 2;
                            stripeCOGSRecord.TransactionId = record.Transfer;
                            stripeCOGSRecord.SortOrder = 2;
                            oneTransaction.Add(stripeCOGSRecord);

                            // now subtract the cost of the sold stuff from inventory
                            StripeModels.OutputData stripeInventoryRecord = new();
                            stripeInventoryRecord.Date = DateOnly.FromDateTime(record.Available_On);
                            stripeInventoryRecord.Account = "Assets:INVENTORY";
                            stripeInventoryRecord.Description = $"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid}";
                            stripeInventoryRecord.Amount = record.Amount / 2;
                            stripeInventoryRecord.TransactionId = record.Transfer;
                            stripeInventoryRecord.SortOrder = 3;
                            oneTransaction.Add(stripeInventoryRecord);


                            stripeFeeRecord.Date = DateOnly.FromDateTime(record.Available_On);
                            stripeFeeRecord.Account = "Expenses:StripeCC Fees";
                            stripeFeeRecord.Description = string.Empty;// $"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid} Stripe Fee";
                            stripeFeeRecord.Amount = -record.Fee;
                            stripeFeeRecord.TransactionId = record.Transfer;
                            stripeFeeRecord.SortOrder = 4;
                            oneTransaction.Add(stripeFeeRecord);
                            break;
                        case "payout":

                            stripePayoutRecord.Date = DateOnly.FromDateTime(record.Available_On);
                            stripePayoutRecord.Account = "Assets:Current Assets:TCCU Business Checking";
                            stripePayoutRecord.Description = string.Empty;//$"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid} XFER to Checking";
                            stripePayoutRecord.Amount = record.Amount;
                            stripePayoutRecord.TransactionId = record.Transfer;
                            stripePayoutRecord.SortOrder = 5;
                            oneTransaction.Add(stripePayoutRecord);
                            break;
                        default:
                            break;
                    }
                }

                cleanedRecords.AddRange(from o in oneTransaction orderby o.SortOrder select o);
            }
            return cleanedRecords;
        }

        public async  Task<List<ToGnuCash>> ReformatStripeForGnuCash(
            IEnumerable<StripeModels.Itemized_balance_change_from_activity_USD> incomingRecords)
        {
            var cleanedRecords = new List<ToGnuCash>();
            foreach(var record in incomingRecords)
            {
                List<ToGnuCash> oneTransaction = new List<ToGnuCash>();

                StripeModels.OutputData stripeIncomeLine = new();
                StripeModels.OutputData stripeFeeLine = new();
                StripeModels.OutputData stripePayoutLine = new();
                StripeModels.OutputData payoutLineFrom = new();
                StripeModels.OutputData payoutLineTo = new();


                // process the sale
                // gross income from the sale
                stripeIncomeLine.Date = DateOnly.FromDateTime(DateTime.Parse(record.created));
                stripeIncomeLine.Account = "Income:DSD Website Sales:Stripe CC Sale";
                stripeIncomeLine.Description = $"NopCommerce Order #{record.description}";
                stripeIncomeLine.Amount = Decimal.Parse(record.gross);
                stripeIncomeLine.TransactionId = record.balance_transaction_id;
                stripeIncomeLine.SortOrder = 1;
                oneTransaction.Add(stripeIncomeLine);

                // stripe fees
                stripeFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(record.created));
                stripeFeeLine.Account = "Expenses:StripeCC Fees";
                stripeFeeLine.Description = string.Empty;// $"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid} Stripe Fee";
                stripeFeeLine.Amount = -Decimal.Parse(record.fee);
                stripeFeeLine.TransactionId = record.balance_transaction_id;
                stripeFeeLine.SortOrder = 2;
                oneTransaction.Add(stripeFeeLine);

                // net income
                stripePayoutLine.Date = DateOnly.FromDateTime(DateTime.Parse(record.created));
                stripePayoutLine.Account = "Assets:Current Assets:website";
                stripePayoutLine.Description = string.Empty;//$"NopCommerce Order #{record.Description} - Order GUID {record.OrderGuid} XFER to Checking";
                stripePayoutLine.Amount = -Decimal.Parse(record.net);
                stripePayoutLine.TransactionId = record.balance_transaction_id;
                stripePayoutLine.SortOrder = 3;
                oneTransaction.Add(stripePayoutLine);

                // transfer the net income
                // from stripe
                payoutLineFrom.Account = $"Assets:Current Assets:website";
                payoutLineFrom.Date = DateOnly.FromDateTime(DateTime.Parse(record.available_on));
                payoutLineFrom.Description = $"NopCommerce Order #{record.description}";
                payoutLineFrom.Amount = decimal.Parse(record.net);
                payoutLineFrom.TransactionId = record.balance_transaction_id;
                payoutLineFrom.SortOrder = 1;
                oneTransaction.Add(payoutLineFrom);

                // to checking
                payoutLineTo.Account = "Assets:Current Assets:TCCU Business Checking";
                payoutLineTo.Date = DateOnly.FromDateTime(DateTime.Parse(record.available_on));
                payoutLineTo.Description = string.Empty; //  payout.Description;
                payoutLineTo.Amount = -decimal.Parse(record.net);
                payoutLineTo.TransactionId = record.balance_transaction_id;
                payoutLineTo.SortOrder = 2;
                oneTransaction.Add(payoutLineTo);

                // Define COGS as 1/2 of sale price
                // And add to the cost of goods sold
                StripeModels.OutputData stripeCOGSRecord = new();
                stripeCOGSRecord.Date = DateOnly.FromDateTime(DateTime.Parse(record.created));
                stripeCOGSRecord.Account = "Expenses:Cost of Goods Sold";
                stripeCOGSRecord.Description = $"NopCommerce Order #{record.description}";// - Order GUID {record.OrderGuid}";
                stripeCOGSRecord.Amount = (await MakeCogsForFullOrder(record));//Decimal.Parse(record.gross) / 2;
                stripeCOGSRecord.TransactionId = record.balance_transaction_id;
                stripeCOGSRecord.SortOrder = 2;
                oneTransaction.Add(stripeCOGSRecord);

                // now subtract the cost of the sold stuff from inventory
                StripeModels.OutputData stripeInventoryRecord = new();
                stripeInventoryRecord.Date = DateOnly.FromDateTime(DateTime.Parse(record.created));
                stripeInventoryRecord.Account = "Assets:INVENTORY";
                stripeInventoryRecord.Description = $"NopCommerce Order #{record.description}";
                stripeInventoryRecord.Amount = -stripeCOGSRecord.Amount;// Decimal.Parse(record.gross) / 2;
                stripeInventoryRecord.TransactionId = record.balance_transaction_id;
                stripeInventoryRecord.SortOrder = 3;
                oneTransaction.Add(stripeInventoryRecord);


                //cleanedRecords.AddRange(from o in oneTransaction
                //                      orderby o.SortOrder
                //                    select o);
                cleanedRecords.AddRange(oneTransaction);
            }
            return  cleanedRecords;
        }

        private  async Task<decimal> MakeCogsForFullOrder(StripeModels.Itemized_balance_change_from_activity_USD record)
        {
            var sellingPrice = Decimal.Parse(record.gross);
            decimal fullOrderCogs = 0.0m;
            if(sellingPrice == 130.0m)
            {
                return fullOrderCogs;
            }
            var skus = await new StripeCleaner().PullOutSkus(record.description); // Fixed: Create an instance of StripeCleaner to call the non-static method
            foreach(var sku in skus)
            {
                var singleStampCogs = db.GetStampCostById(int.Parse(sku));
                if((singleStampCogs == null) || (singleStampCogs == 0.0m))
                {
                    singleStampCogs = -(sellingPrice / 2);
                } else
                {
                    singleStampCogs = (decimal)-(singleStampCogs);
                }
                fullOrderCogs += (decimal)singleStampCogs;
            }
            return fullOrderCogs;
        }

        private async Task<List<string>> PullOutSkus(string NopOrderId)
        {
            return await db.GetSoldStamps(NopOrderId); // Use instance field `db` directly
        }
        #endregion
    }
}
