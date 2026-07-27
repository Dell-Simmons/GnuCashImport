using EbaySharp.Entities.Common;
using FeeBayConnectionTester.DTO;
using SimpleFin.SimpleFinDTO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace FeeBayConnectionTester.Services.PeakCreditUnion
{
    public class PeakCUTransactionProcessor : IPeakCUTransactionProcessor
    {
        public async Task<List<PeakCUToGnuCash>> ProcessPeakCuTransactionsAsync(List<SimpleFinTransaction> peakCuTransactions)
        {
            var cleanEntries = new List<PeakCUToGnuCash>();
            foreach (var record in peakCuTransactions)
            {
                IList<PeakCUToGnuCash> oneTransaction = new List<PeakCUToGnuCash>();
                if (record.Amount < 0)
                {
                    PeakCUToGnuCash peakCUDebitRecord = new();
                    peakCUDebitRecord.Date = DateOnly.FromDateTime(record.PostedDate.DateTime);// (record.PostedDate);
                    peakCUDebitRecord.Account = "Assetts:Current Assetts:TCCU Business Checking";
                    peakCUDebitRecord.Description = record.Description;
                    peakCUDebitRecord.Amount = -record.Amount;
                    peakCUDebitRecord.SortOrder = 1;
                    peakCUDebitRecord.TransactionId = record.TransactionId;
                    peakCUDebitRecord.CheckNumber = ExtractCheckNumber(record);
                    oneTransaction.Add(peakCUDebitRecord);

                    PeakCUToGnuCash peakCUExpenceRecord = new();
                    peakCUExpenceRecord.Date = DateOnly.FromDateTime(record.PostedDate.DateTime);
                    peakCUExpenceRecord.Account = SetCorrectExpenseAccount(record);
                    peakCUExpenceRecord.Description = string.Empty;// record.Description;
                    peakCUExpenceRecord.Amount = (record.Amount);
                    peakCUExpenceRecord.TransactionId = record.TransactionId;
                    peakCUExpenceRecord.SortOrder = 2;
                    oneTransaction.Add(peakCUExpenceRecord);

                    cleanEntries.AddRange(oneTransaction);
                }
                if (record.Amount > 0)
                {
                    PeakCUToGnuCash peakCUCreditRecord = new();
                    peakCUCreditRecord.Date = DateOnly.FromDateTime(record.PostedDate.DateTime);
                    peakCUCreditRecord.Account = "Assetts:Current Assetts:TCCU Business Checking";
                    peakCUCreditRecord.Description = record.Description;
                    peakCUCreditRecord.Amount = -(record.Amount);
                    peakCUCreditRecord.TransactionId = record.TransactionId;
                    peakCUCreditRecord.SortOrder = 1;
                    oneTransaction.Add(peakCUCreditRecord);

                    PeakCUToGnuCash peakCUCreditSourceRecord = new();
                    peakCUCreditSourceRecord.Date = DateOnly.FromDateTime(record.PostedDate.DateTime);
                    peakCUCreditSourceRecord.Account = SetCorrectIncomeAccount(record);
                    peakCUCreditSourceRecord.Description = string.Empty;// record.Description;
                    peakCUCreditSourceRecord.Amount = (record.Amount);
                    peakCUCreditSourceRecord.TransactionId = record.TransactionId;
                    peakCUCreditSourceRecord.SortOrder = 2;
                    oneTransaction.Add(peakCUCreditSourceRecord);

                    cleanEntries.AddRange(oneTransaction);
                }
            } 
            return cleanEntries;//new List<ToGnuCash>();
        }
        private int ExtractCheckNumber(SimpleFinTransaction record)
        {
            if (string.IsNullOrEmpty(record.Payee))
            {
                return 0;
            }

            Match match = Regex.Match(record.Payee, @"\d+");

            if (match.Success && int.TryParse(match.Value, out int number))
            {
                return number;
            }

            return 0;
        }
        private string SetCorrectIncomeAccount(SimpleFinTransaction record)
        {
            if(record == null)
            {
                return string.Empty;
            }
            if (record.Amount < 0) 
            {
                return string.Empty;
            }
            if (record.Payee.Equals("Deposit by Check", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Incoming Cash:website phone orders";
            }   
            if (record.Payee.Equals("Stripe", StringComparison.InvariantCultureIgnoreCase)) 
            {
                return "Incoming Cash:website";
            }
            if (record.Payee.Equals("eBay", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Incoming Cash:feeBay SI";
            }
            if( record.Description.Contains("DEPOSIT", StringComparison.InvariantCultureIgnoreCase) &&
                record.Description.Contains("STRIPE", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Incoming Cash:website";
            }
            if (record.Description.Contains("DEPOSIT", StringComparison.InvariantCultureIgnoreCase) &&
                record.Description.Contains("EBAY", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Incoming Cash:feeBay SI";
            }
            return "Expences:Unknown";
        }
        private string SetCorrectExpenseAccount(SimpleFinTransaction record)
        {
            if(record == null)
            {
                return string.Empty;
            }
            if(record.Amount < 0 && record.Description.Contains("CAPITAL ONE", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Credit Card Liabilities:Capital One Spark Business Credit Card";
            }
            if(record.Amount <0 && record.Description.Contains("Check", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Expences:Miscellaneous";
            }
            // TODO: Implement logic to determine correct expense account based on description
            return "Expenses:Unknown";
        }
    }
}
