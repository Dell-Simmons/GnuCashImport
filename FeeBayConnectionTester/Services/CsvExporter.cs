using FeeBayConnectionTester.DTO;
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

        public static void WriteIncomingDataToCsv(List<ToGnuCash> rows, string outputPath)
        {
            if (rows == null || !rows.Any())
            {
                throw new ArgumentException("Incoming data list cannot be null or empty", nameof(rows));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));
            }

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(true));

            writer.WriteLine("Date,Account,Description,Amount,TransactionId,Sort");

            foreach (var row in rows)
            {
                writer.WriteLine(string.Join(",", new[]
                {
                    EscapeCsvField(row.Date.ToString()),
                    EscapeCsvField(row.Account),
                    EscapeCsvField(row.Description),
                    EscapeCsvField(row.Amount.ToString()),//),
                   // EscapeCsvField(row.Amount.ToString("F3",CultureInfo.InvariantCulture)),
                    EscapeCsvField(row.TransactionId),
                    EscapeCsvField(row.SortOrder.ToString())
                    // EscapeCsvField(row.Below_standard_performance_fee),
                    // EscapeCsvField(row.Buyer_name),
                    // EscapeCsvField(row.Buyer_username),
                    // EscapeCsvField(row.Charity_donation),
                    // EscapeCsvField(row.Deposit_processing_fee),
                    // EscapeCsvField(row.Description),
                    // EscapeCsvField(row.Exchange_rate),
                    // EscapeCsvField(row.feeBay_collected_tax),
                    // EscapeCsvField(row.FVF_fixed),
                    // EscapeCsvField(row.FVF_variable),
                    // EscapeCsvField(row.Gross_transaction_amount),
                    // EscapeCsvField(row.International_fee),
                    // EscapeCsvField(row.Item_ID),
                    // EscapeCsvField(row.Item_not_as_described_fee),
                    // EscapeCsvField(row.Item_subtotal),
                    // EscapeCsvField(row.Item_title),
                    // EscapeCsvField(row.Legacy_order_ID),
                    // EscapeCsvField(row.Net_amount),
                    // EscapeCsvField(row.Order_number),
                    // EscapeCsvField(row.Payout_currency),
                    // EscapeCsvField(row.Payout_date),
                    // EscapeCsvField(row.Payout_ID),
                    // EscapeCsvField(row.Payout_method),
                    // EscapeCsvField(row.Payout_status),
                    // EscapeCsvField(row.Quantity),
                    // EscapeCsvField(row.Reason_for_hold),
                    // EscapeCsvField(row.Reference_ID),
                    // EscapeCsvField(row.Regulatory_operating_fee),
                    // EscapeCsvField(row.Seller_collected_tax),
                    // EscapeCsvField(row.Ship_to_city),
                    // EscapeCsvField(row.Ship_to_country),
                    // EscapeCsvField(row.Ship_to_state),
                    // EscapeCsvField(row.Ship_to_zip),
                    // EscapeCsvField(row.Shipping_and_handling),
                    // EscapeCsvField(row.Sku),
                    // EscapeCsvField(row.Transaction_creation_date),
                    // EscapeCsvField(row.Transaction_currency),
                    // EscapeCsvField(row.Transaction_ID),
                    // EscapeCsvField(row.Type)
                }));
            }

            Console.WriteLine($"Successfully wrote {rows.Count} incoming rows to {outputPath}");
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
