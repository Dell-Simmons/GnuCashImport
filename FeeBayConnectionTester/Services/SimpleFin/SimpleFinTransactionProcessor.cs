using EbaySharp.Entities.Develop.SellingApps.OrderManagement.Fulfillment.Order;
using FeeBayConnectionTester.DTO;
using LocalDBConnections;
using SimpleFin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFin.SimpleFinDTO;

namespace FeeBayConnectionTester.Services.SimpleFin
{       
    public class SimpleFinTransactionProcessor : ISimpleFinTransactionProcessor
    {
        public async Task<List<ToGnuCash>> ProcessPeakCuTransactionsAsync(List<SimpleFinTransaction> peakCuTransactions)
        {
            return new List<ToGnuCash>();
        }
        public async Task<List<ToGnuCash>> ProcessSparkCCTransactionsAsync(List<SimpleFinTransaction> incomingRecords)
        {
            var cleanedRecords = new List<ToGnuCash>();
            foreach (var record in incomingRecords)
            {
                IList<ToGnuCash> oneTransaction = new List<ToGnuCash>();
                if (record.Amount < 0)
                {
                    ToGnuCash sparkCCLiabilityRecord = new();
                    sparkCCLiabilityRecord.Date = DateOnly.FromDateTime(record.PostedDate.DateTime);// (record.PostedDate);
                    sparkCCLiabilityRecord.Account = "Credit Card Liabilities:Spark Business Credit Card";
                    sparkCCLiabilityRecord.Description = record.Description;
                    sparkCCLiabilityRecord.Amount = record.Amount;
                    sparkCCLiabilityRecord.SortOrder = 1;
                    sparkCCLiabilityRecord.TransactionId = record.TransactionId;
                    oneTransaction.Add(sparkCCLiabilityRecord);

                    ToGnuCash sparkCCExpenseRecord = new();
                    sparkCCExpenseRecord.Date = DateOnly.FromDateTime(record.PostedDate.DateTime);
                    sparkCCExpenseRecord.Account = SetCorrectExpenseAccount(record.Description);
                    sparkCCExpenseRecord.Description = record.Description;
                    sparkCCExpenseRecord.Amount = -(record.Amount);
                    sparkCCExpenseRecord.TransactionId = record.TransactionId;
                    sparkCCExpenseRecord.SortOrder = 2;
                    oneTransaction.Add(sparkCCExpenseRecord);
                }

                if (record.Amount > 0 && record.Description == "CASH BACK" && record.Payee == "Cashback")
                {
                    ToGnuCash sparkCCCreditRecord = new();
                    sparkCCCreditRecord.Date = DateOnly.FromDateTime(record.PostedDate.DateTime);
                    sparkCCCreditRecord.Account = "Credit Card Liabilities:Spark Business Credit Card";
                    sparkCCCreditRecord.Description = record.Description;
                    sparkCCCreditRecord.Amount = record.Amount;
                    sparkCCCreditRecord.TransactionId = record.TransactionId;
                    sparkCCCreditRecord.SortOrder = 1;
                    oneTransaction.Add(sparkCCCreditRecord);

                    ToGnuCash sparkCCIncomeRecord = new();
                    sparkCCCreditRecord.Date = DateOnly.FromDateTime(record.PostedDate.DateTime);
                    sparkCCIncomeRecord.Account = "Spark CC Cash Back";
                    sparkCCIncomeRecord.Description = record.Description;
                    sparkCCIncomeRecord.Amount = -record.Amount;
                    sparkCCIncomeRecord.TransactionId = record.TransactionId;
                    sparkCCIncomeRecord.SortOrder = 2;
                    oneTransaction.Add(sparkCCIncomeRecord);
                }
                if (record.Amount > 0 && record.Description == "ELECTRONIC PAYMENT" && record.Payee == "Electronic Payment")
                {
                    ToGnuCash sparkCCCreditRecord = new();
                    sparkCCCreditRecord.Date = DateOnly.FromDateTime(record.PostedDate.DateTime);
                    sparkCCCreditRecord.Account = "Credit Card Liabilities:Spark Business Credit Card";
                    sparkCCCreditRecord.Description = record.Description;
                    sparkCCCreditRecord.Amount = record.Amount;
                    sparkCCCreditRecord.TransactionId = record.TransactionId;
                    sparkCCCreditRecord.SortOrder = 1;
                    oneTransaction.Add(sparkCCCreditRecord);

                    ToGnuCash sparkCCIncomeRecord = new();
                    sparkCCIncomeRecord.Date = DateOnly.FromDateTime(record.PostedDate.DateTime);
                    sparkCCIncomeRecord.Account = "TCCU Business Checking";
                    sparkCCIncomeRecord.Description = record.Description;
                    sparkCCIncomeRecord.Amount = -record.Amount;
                    sparkCCIncomeRecord.TransactionId = record.TransactionId;
                    sparkCCIncomeRecord.SortOrder = 2;
                    oneTransaction.Add(sparkCCIncomeRecord);
                }
                cleanedRecords.AddRange(oneTransaction);
            }

            return cleanedRecords;
        }

        private static string SetCorrectExpenseAccount(string description)
        {
            switch (description.Substring(0, 6).ToLower())
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
                    if (description.Equals("paypal *zenstudiesw", StringComparison.CurrentCultureIgnoreCase))
                        return "Expenses:Daiho Zen";
                    if (description.Equals("paypal *kelleherauc", StringComparison.CurrentCultureIgnoreCase))
                        return "Assets:Inventory";
                    if (description.Equals("paypal *americanrev", StringComparison.CurrentCultureIgnoreCase))
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
