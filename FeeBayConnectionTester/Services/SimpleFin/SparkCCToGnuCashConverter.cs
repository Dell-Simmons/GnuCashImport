using FeeBayConnectionTester.DTO;
using SimpleFin.SimpleFinDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFin
{
    internal static class SparkCCToGnuCashConverter
    {
        public static IEnumerable<ToGnuCash> ReformatSparkCCDataForGnuCash(IEnumerable<SimpleFinTransaction> incomingRecords)
        {
            var cleanedRecords = new List<ToGnuCash>();
            foreach (var record in incomingRecords)
            {
                IList<ToGnuCash> oneTransaction = new List<ToGnuCash>();
                if(record.Amount < 0)
                {
                    ToGnuCash sparkCCLiabilityRecord = new();
                    sparkCCLiabilityRecord.Date = DateOnly.FromDateTime(record.PostedDate.DateTime);// (record.PostedDate);
                    sparkCCLiabilityRecord.Account = "Credit Card Liabilities:Spark Business Credit Card";
                    sparkCCLiabilityRecord.Description = record.Description;
                    sparkCCLiabilityRecord.Amount = record.Amount;
                    sparkCCLiabilityRecord.SortOrder = 1;
                    sparkCCLiabilityRecord.TransactionId = record.TransactionId;
                    oneTransaction.Add(sparkCCLiabilityRecord);

                    Stripe.StripeModels.OutputData sparkCCExpenseRecord = new();
                    sparkCCExpenseRecord.Date = DateOnly.FromDateTime(DateTime.Parse(record.Posted_Date));
                    sparkCCExpenseRecord.Account = SetCorrectExpenseAccount(record.Description);
                    sparkCCExpenseRecord.Description = record.Description;
                    sparkCCExpenseRecord.Amount = -decimal.Parse(record.Debit);
                    sparkCCExpenseRecord.TransactionId = string.Empty;
                    sparkCCExpenseRecord.SortOrder = 2;
                    oneTransaction.Add(sparkCCExpenseRecord);
                }
                
                if(record.Credit != string.Empty && record.Category == "Payment/Credit"  && record.Description =="CASH AUTO REDEMPTION")
                {
                    Stripe.StripeModels.OutputData sparkCCCreditRecord = new();
                    sparkCCCreditRecord.Date = DateOnly.FromDateTime(DateTime.Parse(record.Posted_Date));
                    sparkCCCreditRecord.Account = "Credit Card Liabilities:Spark Business Credit Card";
                    sparkCCCreditRecord.Description = record.Description;
                    sparkCCCreditRecord.Amount = decimal.Parse(record.Credit);
                    sparkCCCreditRecord.TransactionId = string.Empty;
                    sparkCCCreditRecord.SortOrder = 1;
                    oneTransaction.Add(sparkCCCreditRecord);

                    Stripe.StripeModels.OutputData sparkCCIncomeRecord = new();
                    sparkCCCreditRecord.Date = DateOnly.FromDateTime(DateTime.Parse(record.Posted_Date));
                    sparkCCIncomeRecord.Account = "Spark CC Cash Back";
                    sparkCCIncomeRecord.Description = record.Description;
                    sparkCCIncomeRecord.Amount = -decimal.Parse(record.Credit);
                    sparkCCIncomeRecord.TransactionId = string.Empty;
                    sparkCCIncomeRecord.SortOrder = 2;
                    oneTransaction.Add(sparkCCIncomeRecord);
                }
                if(record.Credit != string.Empty && record.Category == "Payment/Credit" && record.Description == "ELECTRONIC PAYMENT")
                {
                    Stripe.StripeModels.OutputData sparkCCCreditRecord = new();
                    sparkCCCreditRecord.Date = DateOnly.FromDateTime(DateTime.Parse(record.Posted_Date));
                    sparkCCCreditRecord.Account = "Credit Card Liabilities:Spark Business Credit Card";
                    sparkCCCreditRecord.Description = record.Description;
                    sparkCCCreditRecord.Amount = decimal.Parse(record.Credit);
                    sparkCCCreditRecord.TransactionId = string.Empty;
                    sparkCCCreditRecord.SortOrder = 1;
                    oneTransaction.Add(sparkCCCreditRecord);

                    Stripe.StripeModels.OutputData sparkCCIncomeRecord = new();
                    sparkCCIncomeRecord.Date = DateOnly.FromDateTime(DateTime.Parse(record.Posted_Date));
                    sparkCCIncomeRecord.Account = "TCCU Business Checking";
                    sparkCCIncomeRecord.Description = record.Description;
                    sparkCCIncomeRecord.Amount = -decimal.Parse(record.Credit);
                    sparkCCIncomeRecord.TransactionId = string.Empty;
                    sparkCCIncomeRecord.SortOrder = 2;
                    oneTransaction.Add(sparkCCIncomeRecord);
                }
                cleanedRecords.AddRange(oneTransaction);
            }  
            
            return cleanedRecords;
            
        }

        private static string SetCorrectExpenseAccount(string description)
        {
            switch (description.Substring(0,6).ToLower())
            {
                case "uline ":
                    return "Expenses:Office Supplies";
                case "sq *b ":
                    return "Expenses:Office Supplies";
                    case "people":
                        return "Expenses:Website Fees and Expenses:Hosting Fees";
                case "shippo":  
                    return "Expenses:Postage and Delivery";
                case "amazon":
                    return "Expenses:Miscellaneous";
                case "google":
                    return "Expenses:Advertising:Google AdWords";
                    case "paypal":
                    if(description.Equals("paypal *zenstudiesw", StringComparison.CurrentCultureIgnoreCase))
                        return "Expenses:Daiho Zen";
                    if (description.Equals("paypal *kelleherauc", StringComparison.CurrentCultureIgnoreCase))
                        return "Assets:Inventory";
                    if(description.Equals("paypal *americanrev", StringComparison.CurrentCultureIgnoreCase))
                        return "Expenses:Dues and Subscriptions";
                    if (description.Equals("paypal *2checkoutco", StringComparison.CurrentCultureIgnoreCase))
                        return "Expenses:Website Fees and Expenses:Domain renewals";
                    if (description.Equals("paypal *ebay us", StringComparison.CurrentCultureIgnoreCase))
                        return "Expenses:Miscellaneous";
                    else
                        return "Expenses:Miscellaneous";
                default: 
                    return "Expenses:Miscellaneous";
            }
        }
    }
}
