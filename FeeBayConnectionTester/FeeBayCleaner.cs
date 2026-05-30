using GnuCashCSVImporter.FeeBay.FeeBayDataModels;
using GnuCashCSVImporter.Stripe.Models;

namespace GnuCashCSVImporter.FeeBay
{
    public class FeeBayCleaner
    {
        private readonly StampDBService.StampDBConnection _db = new();
        #region Methods
        // https://www.reddit.com/r/GnuCash/comments/16gzkk0/what_account_type_should_ebay_be_now/
        public IEnumerable<Stripe.StripeModels.OutputData> ReformatFeeBayForGnuCash(IEnumerable<IncomingModels.FeeBayDataModels.FeeBayIncomingData> incomingRecords, string feeBayId)
        {
            string feeBayName1 = string.Empty;
            string feeBayName2 = string.Empty;
            if (feeBayId.Equals("simmons ink", StringComparison.CurrentCultureIgnoreCase))
            {
                feeBayName1 = "Simmons_Ink";
                feeBayName2 = "feeBay SI";
            }
            if (feeBayId.Equals("duckstampdealer", StringComparison.CurrentCultureIgnoreCase))
            {
                feeBayName1 = "DuckStampDealer";
                feeBayName2 = "feeBay DSD";
            }
            var outputData = new List<Stripe.StripeModels.OutputData>();

            var tempOrders = incomingRecords.Where(x => x.Type.Equals("order", StringComparison.CurrentCultureIgnoreCase));
            // some tempOrders have more than one item per order . . . 
            var orders = from record in tempOrders group record by record.Order_number into newGroup select newGroup;
            var refunds = incomingRecords.Where(x => x.Type.Equals("refund", StringComparison.CurrentCultureIgnoreCase));
            var claims = incomingRecords.Where(x => x.Type.Equals("claim", StringComparison.CurrentCultureIgnoreCase));
            var disputes = incomingRecords.Where(x => x.Type.Equals("payment dispute", StringComparison.CurrentCultureIgnoreCase));
            var shippingLabels = incomingRecords.Where(x => x.Type.Equals("shipping label", StringComparison.CurrentCultureIgnoreCase));
            var charges = incomingRecords.Where(x => x.Type.Equals("charge", StringComparison.CurrentCultureIgnoreCase));
            var transfers = incomingRecords.Where(x => x.Type.Equals("transfer", StringComparison.CurrentCultureIgnoreCase));
            var holds = incomingRecords.Where(x => x.Type.Equals("hold", StringComparison.CurrentCultureIgnoreCase));
            var otherFees = incomingRecords.Where(x => x.Type.Equals("other fee", StringComparison.CurrentCultureIgnoreCase));
            var adjustments = incomingRecords.Where(x => x.Type.Equals("adjustment", StringComparison.CurrentCultureIgnoreCase));
            var purchases = incomingRecords.Where(x => x.Type.Equals("purchase", StringComparison.CurrentCultureIgnoreCase));
            var payouts = incomingRecords.Where(x => x.Type.Equals("payout", StringComparison.CurrentCultureIgnoreCase));
            var secondaryPayouts = incomingRecords.Where(x => x.Type.Equals("secondary payout", StringComparison.CurrentCultureIgnoreCase));
            var withheldTaxes = incomingRecords.Where(x => x.Type.Equals("withheld tax", StringComparison.CurrentCultureIgnoreCase));
            var reserves = incomingRecords.Where(x => x.Type.Equals("reserve", StringComparison.CurrentCultureIgnoreCase));

            foreach (var order in orders)
            {
                var skusInOrder = PullOutSkus(order);
                var orderDate = order.First().Transaction_creation_date;
                var orderId = order.First().Order_number;
                var sellingPrice = order.Sum(x => decimal.Parse(x.Item_subtotal));
                var shippingPrice = order.Sum(x => decimal.Parse(x.Shipping_and_handling));
                var fixedFees = order.Sum(x => decimal.Parse(x.FVF_fixed));
                var variableFees = order.Sum(x => decimal.Parse(x.FVF_variable));
                var net = order.Sum(x => decimal.Parse(x.Net_amount));
                var internationalFees = order.Sum(x => decimal.Parse(x.International_fee));
                var gross = order.Sum(x => decimal.Parse(x.Gross_transaction_amount));
                var numberSold = order.Count(x => !string.Equals(x.Sku, "--", StringComparison.Ordinal));
                if (sellingPrice + shippingPrice != gross)
                {
                    MessageBox.Show(
                        $"Gross amount {gross} does not equal selling price {sellingPrice} + shipping price {shippingPrice}");
                }
                if (gross + fixedFees + variableFees + internationalFees != net)
                {
                    MessageBox.Show(
                        $"Net amount {net} does not equal gross amount {gross} - fixed fees {fixedFees} - variable fees {variableFees}");
                }
                var incomeLineDescription = string.Empty;
                if (numberSold == 1)
                {
                    incomeLineDescription = $"feeBay Order #{orderId} - {order.First().Item_title}";
                }
                else
                {
                    incomeLineDescription = $"feeBay Order #{orderId} - {numberSold} items sold";
                }

                // income line
                try
                {
                    var incomeLine = new Stripe.StripeModels.OutputData();
                    incomeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                    incomeLine.Account = $"Income:{feeBayName2} Sales";
                    incomeLine.Description = incomeLineDescription;
                    incomeLine.Amount = sellingPrice + shippingPrice;
                    incomeLine.TransactionId = orderId;
                    incomeLine.SortOrder = 1;
                    outputData.Add(incomeLine);
                }
                catch (Exception)
                {
                    throw;
                }

                // fixed fee Line
                try
                {
                    var fixedFeeLine = new Stripe.StripeModels.OutputData();
                    fixedFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                    fixedFeeLine.Account = $"Expences:FeeBay Fees:{feeBayName1}:Fixed Fee Per Sale";
                    fixedFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                    fixedFeeLine.Amount = fixedFees;
                    fixedFeeLine.TransactionId = orderId;
                    fixedFeeLine.SortOrder = 2;
                    outputData.Add(fixedFeeLine);
                }
                catch (Exception)
                {
                    throw;
                }

                // variable fee line
                try
                {
                    var variableFeeLine = new Stripe.StripeModels.OutputData();
                    variableFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                    variableFeeLine.Account = $"Expenses:FeeBay Fees:{feeBayName1}:Final Value Fees";
                    variableFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                    variableFeeLine.Amount = variableFees;
                    variableFeeLine.TransactionId = orderId;
                    variableFeeLine.SortOrder = 3;
                    outputData.Add(variableFeeLine);
                }
                catch (Exception)
                {
                    throw;
                }

                //add the remaining money to feeBay current assett
                try
                {
                    var netIncomeLine = new Stripe.StripeModels.OutputData();
                    netIncomeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                    netIncomeLine.Account = $"Assets:Current Assets:feeBay:{feeBayName2}";
                    netIncomeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                    netIncomeLine.Amount = -net;
                    netIncomeLine.TransactionId = orderId;
                    netIncomeLine.SortOrder = 4;
                    outputData.Add(netIncomeLine);
                }
                catch (Exception)
                {
                    throw;
                }

                //international fees line
                try
                {
                    if (internationalFees != 0)
                    {
                        var internationalFeeLine = new Stripe.StripeModels.OutputData();
                        internationalFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                        internationalFeeLine.Account = $"Expenses:FeeBay Fees:{feeBayName1}:International Fee";
                        internationalFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                        internationalFeeLine.Amount = internationalFees;
                        internationalFeeLine.TransactionId = orderId;
                        internationalFeeLine.SortOrder = 5;
                        outputData.Add(internationalFeeLine);
                    }
                }
                catch (Exception)
                {
                    throw;
                }

                // And add to the cost of goods sold
                try
                {
                    Stripe.StripeModels.OutputData feeBayCOGSRecord = new();
                    feeBayCOGSRecord.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                    feeBayCOGSRecord.Account = "Expenses:Cost of Goods Sold";
                    feeBayCOGSRecord.Description = $"{incomeLineDescription} COGS";
                    feeBayCOGSRecord.Amount = MakeCogsForFullOrder(sellingPrice, skusInOrder);
                    feeBayCOGSRecord.TransactionId = orderId;
                    feeBayCOGSRecord.SortOrder = 6;
                    outputData.Add(feeBayCOGSRecord);
                }
                catch (Exception)
                {
                    throw;
                }

                // now subtract the cost of the sold stuff from inventory
                try
                {
                    Stripe.StripeModels.OutputData feeBayInventoryRecord = new();
                    feeBayInventoryRecord.Date = DateOnly.FromDateTime(DateTime.Parse(orderDate));
                    feeBayInventoryRecord.Account = "Assets:INVENTORY";
                    feeBayInventoryRecord.Description = $"{incomeLineDescription} COGS";
                    feeBayInventoryRecord.Amount = -MakeCogsForFullOrder(sellingPrice, skusInOrder);
                    feeBayInventoryRecord.TransactionId = orderId;
                    feeBayInventoryRecord.SortOrder = 7;
                    outputData.Add(feeBayInventoryRecord);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            foreach (var refund in refunds)
            {
                // this works leave it alone
                //! Consider inputing all csv numbers as absolute values . . .
                //! Then can use + or - as needed without worrying about positives or negatives in input
                //
                try
                {
                    var cogs = _db.GetStampCostById(int.Parse(refund.Sku));
                    var refundDate = refund.Payout_date;
                    var orderId = refund.Order_number;
                    var sellingPriceRefunded = Decimal.Parse(refund.Net_amount);
                    var fixedFeesRefunded = Decimal.Parse(refund.FVF_fixed);
                    var variableFeesRefunded = Decimal.Parse(refund.FVF_variable);
                    var netRefund = Decimal.Parse(refund.Net_amount);
                    var internationalFeesRefunded = Decimal.Parse(refund.International_fee);
                    var grossRefund = Decimal.Parse(refund.Gross_transaction_amount);

                    // income line
                    var incomeLine = new Stripe.StripeModels.OutputData();
                    incomeLine.Date = DateOnly.FromDateTime(DateTime.Parse(refundDate));
                    incomeLine.Account = $"Income:{feeBayName2} Sales";
                    incomeLine.Description = $"feeBay Order #{orderId} - {refund.Item_title} REFUNDED";
                    incomeLine.Amount = grossRefund;// + shippingPrice;
                    incomeLine.TransactionId = orderId;
                    incomeLine.SortOrder = 1;
                    outputData.Add(incomeLine);

                    // feeBay current assett
                    var netIncomeLine = new Stripe.StripeModels.OutputData();
                    netIncomeLine.Date = DateOnly.FromDateTime(DateTime.Parse(refundDate));
                    netIncomeLine.Account = $"Assets:Current Assets:feeBay:{feeBayName2}";
                    netIncomeLine.Description = string.Empty;
                    netIncomeLine.Amount = -netRefund;
                    netIncomeLine.TransactionId = orderId;
                    netIncomeLine.SortOrder = 2;
                    outputData.Add(netIncomeLine);

                    // fixed fee Line
                    var fixedFeeLine = new Stripe.StripeModels.OutputData();
                    fixedFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(refundDate));
                    fixedFeeLine.Account = $"Expences:FeeBay Fees:{feeBayName1}:Fixed Fee Per Sale";
                    fixedFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                    fixedFeeLine.Amount = fixedFeesRefunded;
                    fixedFeeLine.TransactionId = orderId;
                    fixedFeeLine.SortOrder = 3;
                    outputData.Add(fixedFeeLine);

                    // variable fee line
                    var variableFeeLine = new Stripe.StripeModels.OutputData();
                    variableFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(refundDate));
                    variableFeeLine.Account = $"Expenses:FeeBay Fees:{feeBayName1}:Final Value Fees";
                    variableFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                    variableFeeLine.Amount = variableFeesRefunded;
                    variableFeeLine.TransactionId = orderId;
                    variableFeeLine.SortOrder = 4;
                    outputData.Add(variableFeeLine);

                    //international fees line
                    if (internationalFeesRefunded != 0)
                    {
                        var internationalFeeLine = new Stripe.StripeModels.OutputData();
                        internationalFeeLine.Date = DateOnly.FromDateTime(DateTime.Parse(refundDate));
                        internationalFeeLine.Account = $"Expenses:FeeBay Fees:{feeBayName1}:International Fee";
                        internationalFeeLine.Description = string.Empty;// $"feeBay Order #{orderId} - {numberSold} items sold";
                        internationalFeeLine.Amount = internationalFeesRefunded;
                        internationalFeeLine.TransactionId = orderId;
                        internationalFeeLine.SortOrder = 5;
                        outputData.Add(internationalFeeLine);
                    }

                    // Define COGS as 1/2 of sale price
                    // And add to the cost of goods sold
                    Stripe.StripeModels.OutputData feeBayCOGSRecord = new();
                    feeBayCOGSRecord.Date = DateOnly.FromDateTime(DateTime.Parse(refundDate));
                    feeBayCOGSRecord.Account = "Expenses:Cost of Goods Sold";
                    feeBayCOGSRecord.Description = string.Empty;// $"feeBay Order #{orderId} - {refund.Item_title} REFUNDED COGS";
                    if ((cogs == null) || (cogs == 0.0m))
                    {
                        feeBayCOGSRecord.Amount = -(sellingPriceRefunded / 2);
                    }
                    else
                    {
                        feeBayCOGSRecord.Amount = (decimal)-(cogs);
                    }
                    feeBayCOGSRecord.TransactionId = orderId;
                    feeBayCOGSRecord.SortOrder = 6;
                    outputData.Add(feeBayCOGSRecord);

                    // now subtract the cost of the sold stuff from inventory
                    Stripe.StripeModels.OutputData feeBayInventoryRecord = new();
                    feeBayInventoryRecord.Date = DateOnly.FromDateTime(DateTime.Parse(refundDate));
                    feeBayInventoryRecord.Account = "Assets:INVENTORY";
                    feeBayInventoryRecord.Description = string.Empty;// incomeLineDescription;
                    feeBayInventoryRecord.Amount = -(sellingPriceRefunded / 2);
                    feeBayInventoryRecord.TransactionId = orderId;
                    feeBayInventoryRecord.SortOrder = 7;
                    outputData.Add(feeBayInventoryRecord);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            foreach (var _ in claims) { throw new NotImplementedException("claim"); }
            foreach (var _ in disputes) { throw new NotImplementedException("dispute"); }
            foreach (var label in shippingLabels)
            {
                try
                {
                    var labelLineFrom = new Stripe.StripeModels.OutputData();
                    labelLineFrom.Date = DateOnly.FromDateTime(DateTime.Parse(label.Transaction_creation_date));
                    labelLineFrom.Account = $"Assets:Current Assets:feeBay:{feeBayName2}";
                    labelLineFrom.Description = label.Description;
                    labelLineFrom.Amount = -decimal.Parse(label.Net_amount);
                    labelLineFrom.TransactionId = label.Reference_ID;
                    labelLineFrom.SortOrder = 1;
                    outputData.Add(labelLineFrom);

                    var labelLineTo = new Stripe.StripeModels.OutputData();
                    labelLineTo.Date = DateOnly.FromDateTime(DateTime.Parse(label.Transaction_creation_date));
                    labelLineTo.Account = $"Expenses:Postage and Delivery";
                    labelLineTo.Description = string.Empty; //  payout.Description;
                    labelLineTo.Amount = decimal.Parse(label.Net_amount);
                    labelLineTo.TransactionId = label.Reference_ID;
                    labelLineTo.SortOrder = 2;
                    outputData.Add(labelLineTo);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            foreach (var charge in charges)
            {
                try
                {
                    var chargeLineFrom = new Stripe.StripeModels.OutputData();
                    chargeLineFrom.Date = DateOnly.FromDateTime(DateTime.Parse(charge.Transaction_creation_date));
                    chargeLineFrom.Account = "Assets:Current Assets:TCCU Business Checking";
                    chargeLineFrom.Description = charge.Description;
                    chargeLineFrom.Amount = decimal.Parse(charge.Net_amount);
                    chargeLineFrom.TransactionId = charge.Reference_ID;
                    chargeLineFrom.SortOrder = 1;
                    outputData.Add(chargeLineFrom);

                    var chargeLineTo = new Stripe.StripeModels.OutputData();
                    chargeLineTo.Date = DateOnly.FromDateTime(DateTime.Parse(charge.Transaction_creation_date));
                    chargeLineTo.Account = $"Assets:Incoming Cash:{feeBayName2}";
                    chargeLineTo.Description = string.Empty; //  payout.Description;
                    chargeLineTo.Amount = -decimal.Parse(charge.Net_amount);
                    chargeLineTo.TransactionId = charge.Reference_ID;
                    chargeLineTo.SortOrder = 2;
                    outputData.Add(chargeLineTo);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            foreach (var _ in transfers) { throw new NotImplementedException("transfer"); }
            foreach (var _ in holds)
            {
                // I think you can ignore holds
                //throw new NotImplementedException("hold"); 
            }
            foreach (var otherFee in otherFees)
            {
                try
                {
                    var otherFeeLineFrom = new Stripe.StripeModels.OutputData();
                    otherFeeLineFrom.Date = DateOnly.FromDateTime(DateTime.Parse(otherFee.Transaction_creation_date));
                    otherFeeLineFrom.Account = $"Assets:Current Assets:feeBay:{feeBayName2}";
                    otherFeeLineFrom.Description = otherFee.Description;
                    otherFeeLineFrom.Amount = -decimal.Parse(otherFee.Net_amount);
                    otherFeeLineFrom.TransactionId = otherFee.Reference_ID;
                    otherFeeLineFrom.SortOrder = 1;
                    outputData.Add(otherFeeLineFrom);

                    var otherFeeLineTo = new Stripe.StripeModels.OutputData();
                    otherFeeLineTo.Date = DateOnly.FromDateTime(DateTime.Parse(otherFee.Transaction_creation_date));
                    otherFeeLineTo.Account = $"Expenses:FeeBay Fees:{feeBayName1}:Store Monthly Fee";
                    otherFeeLineTo.Description = string.Empty; //  payout.Description;
                    otherFeeLineTo.Amount = decimal.Parse(otherFee.Net_amount);
                    otherFeeLineTo.TransactionId = otherFee.Reference_ID;
                    otherFeeLineTo.SortOrder = 2;
                    outputData.Add(otherFeeLineTo);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            foreach (var _ in adjustments) { throw new NotImplementedException("adjustment"); }
            foreach (var _ in purchases) { throw new NotImplementedException("purchase"); }
            foreach (var payout in payouts)
            {
                try
                {
                    var payoutLineFrom = new Stripe.StripeModels.OutputData();
                    payoutLineFrom.Account = $"Assets:Current Assets:feeBay:{feeBayName2}";
                    payoutLineFrom.Date = DateOnly.FromDateTime(DateTime.Parse(payout.Transaction_creation_date));
                    payoutLineFrom.Description = payout.Description;
                    payoutLineFrom.Amount = -decimal.Parse(payout.Net_amount);
                    payoutLineFrom.TransactionId = payout.Reference_ID;
                    payoutLineFrom.SortOrder = 1;
                    outputData.Add(payoutLineFrom);

                    var payoutLineTo = new Stripe.StripeModels.OutputData();
                    payoutLineTo.Account = "Assets:Current Assets:TCCU Business Checking";
                    payoutLineTo.Date = DateOnly.FromDateTime(DateTime.Parse(payout.Transaction_creation_date));
                    payoutLineTo.Description = string.Empty; //  payout.Description;
                    payoutLineTo.Amount = decimal.Parse(payout.Net_amount);
                    payoutLineTo.TransactionId = payout.Reference_ID;
                    payoutLineTo.SortOrder = 2;
                    outputData.Add(payoutLineTo);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            foreach (var _ in secondaryPayouts) { throw new NotImplementedException("secondary Payout"); }
            foreach (var _ in withheldTaxes) { throw new NotImplementedException("withheld Tax"); }
            foreach (var _ in reserves) { throw new NotImplementedException("reserve"); }

            return (IEnumerable<Stripe.StripeModels.OutputData>)outputData;
        }

        private decimal MakeCogsForFullOrder(decimal sellingPrice, IList<int> skus)
        {
            decimal fullOrderCogs = 0.0m;
            if (sellingPrice == 130.0m)
            {
                return fullOrderCogs;
            }
            foreach (var sku in skus)
            {
                var singleStampCogs = _db.GetStampCostById(sku);
                if ((singleStampCogs == null) || (singleStampCogs == 0.0m))
                {
                    singleStampCogs = -(sellingPrice / 2);
                }
                else
                {
                    singleStampCogs = (decimal)-(singleStampCogs);
                }
                fullOrderCogs += (decimal)singleStampCogs;
            }
            return fullOrderCogs;
        }

        private List<int> PullOutSkus(IGrouping<string, IncomingModels.FeeBayDataModels.FeeBayIncomingData> order)
        {
            List<int> skus = new();
            foreach (var item in order)
            {
                if (item.Sku != null)
                {
                    var parsedOk = int.TryParse(item.Sku, out int sku);
                    if (parsedOk)
                    {
                        skus.Add(sku);
                    }
                }
            }
            return skus;
        }
        #endregion
    }
}
