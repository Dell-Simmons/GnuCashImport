using Stripe;
using System;
using System.Collections.Generic;
using System.Text;

namespace StripeCCProcessor
{
    public class StripeCCProcessorClient
    {
        public static async Task Run()
        {
            var apiKey = Environment.GetEnvironmentVariable("STRIPE_API_KEY");

            try
            {
                var client = new StripeClient(apiKey);
                Console.WriteLine(await client.V1.Customers.ListAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public async Task<bool> PullSalesAsync(DateTime startDate, DateTime endDate, string stripeSecretKey)
        {
            var client = new Stripe.StripeClient(stripeSecretKey);

            var options = new BalanceTransactionListOptions
            {
                Limit = 100,
                Created = new DateRangeOptions
                {
                    GreaterThanOrEqual = startDate,//new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    LessThanOrEqual = endDate//new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc)
                }
            };

             var look = await client.V1.BalanceTransactions.ListAsync(options);
             
             return true;
        }
    }
}
