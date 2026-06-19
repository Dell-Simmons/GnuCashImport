using EbaySharp.Entities.Common;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances.Transaction;
using System.Globalization;
using System.Linq;

namespace FeeBayConnectionTester.Extensions
{
    public static class TransactionExtensions
    {
        /// <summary>
        /// Gets the total fee amount for a specific fee type from an OrderLineItem's MarketplaceFees collection.
        /// Returns null if no fees of the specified type are found.
        /// Sums multiple fees if more than one of the same type exists.
        /// </summary>
        public static decimal? GetFeeByType(this OrderLineItem lineItem, FeeTypeEnum feeType)
        {
            if (lineItem?.MarketplaceFees == null || !lineItem.MarketplaceFees.Any())
            {
                return null;
            }

            var matchingFees = lineItem.MarketplaceFees
                .Where(f => f.FeeType == feeType)
                .ToList();

            if (!matchingFees.Any())
            {
                return null;
            }

            decimal total = 0;
            foreach (var fee in matchingFees)
            {
                if (fee.Amount?.Value != null && decimal.TryParse(fee.Amount.Value, out decimal feeAmount))
                {
                    total += feeAmount;
                }
            }

            return total;
        }

        /// <summary>
        /// Converts the Amount's Value property to a decimal.
        /// Returns null if the Value is null or cannot be parsed.
        /// </summary>
        public static decimal? DollarAmount(this Amount? amount)
        {
            if (string.IsNullOrWhiteSpace(amount?.Value))
            {
                return null;
            }

            return decimal.TryParse(
                amount.Value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal result)
                ? result
                : null;
        }

        /// <summary>
        /// Converts Amount.Value to decimal, returning fallback when missing or invalid.
        /// </summary>
        public static decimal DollarAmountOrDefault(this Amount? amount, decimal fallback = 0m)
        {
            return amount.DollarAmount() ?? fallback;
        }
    }
}
