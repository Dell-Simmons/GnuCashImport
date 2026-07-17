using System;

namespace FeeBayConnectionTester.Extensions
{
    public static class StripeExtensions
    {
        public static decimal Cents2Dollars(this long cents, decimal fallback = 0m)
        {
            if (cents == null) return fallback;
            if (cents == 0) return 0m;
            return (decimal)cents / 100m;
        }

        public static long Dollars2Cents(this decimal dollars)
        {
            if (dollars == null) return 0;
            if (dollars == 0m) return 0;
            return (long)decimal.Round(dollars * 100m);
        }
    }
}
