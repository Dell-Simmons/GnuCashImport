using System;

namespace FeeBayConnectionTester
{
    /// <summary>
    /// Represents a single line in a GnuCash CSV import file.
    /// Each ToGnuCash record is one split in a multi-split transaction.
    /// </summary>
    public class ToGnuCash
    {
        /// <summary>
        /// Transaction date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// GnuCash account path (e.g., "Income:eBay SI Sales:Product Sale")
        /// </summary>
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// Optional description for this split
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Amount for this split (positive = debit, negative = credit in asset/expense accounts)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Transaction identifier to group related splits together
        /// </summary>
        public string TransactionId { get; set; } = string.Empty;

        /// <summary>
        /// Sort order for display/export (lower numbers first)
        /// </summary>
        public int SortOrder { get; set; }
    }
}
