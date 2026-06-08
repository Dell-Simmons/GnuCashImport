using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace FeeBayConnectionTester.Services
{
    public class CsvExporter
    {
        /// <summary>
        /// Writes ToGnuCash transactions to a CSV file in GnuCash-compatible format.
        /// Sorts by Date then SortOrder, formats dates as yyyy-MM-dd, amounts with 2 decimal places.
        /// Uses UTF-8 encoding with BOM for Excel compatibility.
        /// </summary>
        public static void WriteToCsv(List<ToGnuCash> transactions, string outputPath)
        {
            if (transactions == null || !transactions.Any())
            {
                throw new ArgumentException("Transaction list cannot be null or empty", nameof(transactions));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Sort by Date, then SortOrder
            var sortedTransactions = transactions;
                //.OrderBy(t => t.Date)
                //.ThenBy(t => t.SortOrder)
                //.ToList();

            // Write CSV with UTF-8 encoding and BOM for Excel compatibility
            using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(true));

            // Write GnuCash-compatible headers
            writer.WriteLine("Date,Account,Description,Amount,TransactionId,Sort");

            // Write transaction data
            foreach (var transaction in sortedTransactions)
            {
                var line = FormatCsvLine(transaction);
                writer.WriteLine(line);
            }

            Console.WriteLine($"Successfully wrote {sortedTransactions.Count} transactions to {outputPath}");
        }

        private static string FormatCsvLine(ToGnuCash transaction)
        {
            // Format date as yyyy-MM-dd
            var dateStr = transaction.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // Format amount with 2 decimal places
            var amountStr = transaction.Amount.ToString("F2", CultureInfo.InvariantCulture);

            // Escape fields that might contain commas or quotes
            var account = EscapeCsvField(transaction.Account ?? string.Empty);
            var description = EscapeCsvField(transaction.Description ?? string.Empty);
            var transactionId = EscapeCsvField(transaction.TransactionId ?? string.Empty);
            
            var sort = EscapeCsvField(transaction.SortOrder.ToString());

            return $"{dateStr},{account},{description},{amountStr},{transactionId},{sort}";
        }

        private static string EscapeCsvField(string field)
        {
            // If field contains comma, quote, or newline, wrap in quotes and escape internal quotes
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
    }
}
