using EbaySharp.Controllers;
using EbaySharp.Entities.Common;
using EbaySharp.Entities.Develop.KeyManagement.SigningKey;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances.Payout;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances.Transaction;
using EbaySharp.Entities.Develop.SellingApps.OrderManagement.Fulfillment.Order;
using FeeBayConnectionTester.Extensions;
using FeeBayConnectionTester.Services;
using FeeBayOAuth.TokenService;
using LocalDBConnections;
using LocalDBConnections.StampDataDB.StampDataEntities;
using MicroOrm.Dapper.Repositories.SqlGenerator.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FeeBayConnectionTester
{
    public partial class Form1 : Form
    {
        #region Constants and Fields
        private readonly Func<string, EbayController> _ebayControllerFactory;
        private readonly ILocalDbConnectionManager _localDbConnectionManager;
        private readonly IOAuthTokenService _oAuthTokenService;
        
        private EbayController _eBayController;
        #endregion

        #region Constructors
        public Form1(
            IOAuthTokenService oAuthTokenFactory,
            ILocalDbConnectionManager localDbConnectionManager,
            Func<string, EbayController> ebayControllerFactory)
        {
            InitializeComponent();
            _oAuthTokenService = oAuthTokenFactory;
            _localDbConnectionManager = localDbConnectionManager;
            _ebayControllerFactory = ebayControllerFactory;
        }
        #endregion

        #region Event handlers
        #region Button1 Click Workflow
        private async void button1_Click(object sender, EventArgs e)
        {
            // token identifies the user and application,
            // and is used to authenticate API requests.
            // It is typically obtained through an OAuth flow,
            // where the user grants permission for the application to access their eBay data.
            // The token is then included in the Authorization header of API requests
            // to verify the identity of the requester and ensure they have the necessary
            // permissions to perform the requested actions.
            string? token = await _oAuthTokenService.GetOAuthTokenAsync("Simmons_Ink");

            _eBayController = _ebayControllerFactory(token);
            // The signing key is used to create digital signatures for API requests,
            // it is associated with the application
            // and is used to ensure the integrity and authenticity of the requests.
            // The one stored in the database is good for 3 years from today (5/29/26).
            // So don't fucking worry about it expiring anytime soon.
            var signingKey = await GetOrCreateSigningKey(_eBayController);

            string multiFilter;

            // Combine multiple filters into ONE comma-separated string

            //!{PAYOUT} is funds going from feeBay to bank account
            // multiFilter = "transactionStatus:{PAYOUT},transactionDate:[2026-01-01T00:00:00.000Z..2026-01-31T23:59:59.000Z]";

            //TransactionSummary transactionPayoutSummary = 
            //    await ebayController.GetTransactionSummary(signingKey, multiFilter);
            ////!{ COMPLETED} is funds going from buyer to feeBay.
            //multiFilter = "transactionStatus:{COMPLETED},transactionDate:[2026-01-01T00:00:00.000Z..2026-01-31T23:59:59.000Z]";

            //TransactionSummary transactionCompletedSummary =
            //    await ebayController.GetTransactionSummary(signingKey, multiFilter);

            //!GetTransactions with pagination
            multiFilter = "transactionDate:[2026-01-01T00:00:00.000Z..2026-01-31T23:59:59.000Z]";
            List<Transaction> financialTransactionList = await GetAllTransactionsPaginated(multiFilter, limit: 50);

            //!GetOrders with pagination
            string ordersFilter = "creationdate:[2026-01-01T00:00:00.000Z..2026-01-31T23:59:59.999Z]";
            List<Order> orderList = await GetAllOrdersPaginated(ordersFilter, limit: 50);

            //!Get Payouts (transfers from feeBay to checking from someplace
            //!Extend the Payouts filter by a week to catch payouts from end of month sales
            string payOutsFilter = "payoutDate:[2026-01-01T00:00:00.000Z..2026-02-14T23:59:59.999Z]";
            List<Payout> payOutList = await GetAllPayOutsPaginated(payOutsFilter, limit: 50);


            await FormatToSendToGnuCash(orderList, financialTransactionList, payOutList);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }
        #endregion
        #endregion

        #region Methods
        #region Private Methods
        private async Task<List<Payout>> GetAllPayOutsPaginated(string filter, int limit = 50)
        {
            var allPayouts = new List<Payout>();
            int offset = 0;
            bool hasMore = true;

            while (hasMore)
            {
                // Append offset to filter if not the first page
                string paginatedFilter = offset > 0 ? $"{filter},offset:{offset}" : filter;

                PayoutList payoutsContainer = await _eBayController.GetPayouts(
                    filter, null, limit, offset);


                if (payoutsContainer.Payouts != null && payoutsContainer.Payouts.Any())
                {
                    allPayouts.AddRange(payoutsContainer.Payouts);
                    Console.WriteLine($"Retrieved {payoutsContainer.Payouts.Count} payouts (Total so far: {allPayouts.Count})");
                }

                // Check if there are more pages
                hasMore = !string.IsNullOrEmpty(payoutsContainer.Next);
                offset += limit;

                // Safety check: if we've retrieved all transactions
                if (allPayouts.Count >= payoutsContainer.Total)
                {
                    hasMore = false;
                }
            }

            Console.WriteLine($"Completed pagination. Total payouts retrieved: {allPayouts.Count}");
            return allPayouts;
        }


        private async Task<List<Transaction>> GetAllTransactionsPaginated(string filter, int limit = 50)
        {
            var allTransactions = new List<Transaction>();
            int offset = 0;
            bool hasMore = true;

            while (hasMore)
            {
                // Append offset to filter if not the first page
                string paginatedFilter = offset > 0 ? $"{filter},offset:{offset}" : filter;

                Transactions transactionsContainer = await _eBayController.GetTransactions(
                    filter,null,limit,offset);
                   

                if (transactionsContainer.TransactionList != null && transactionsContainer.TransactionList.Any())
                {
                    allTransactions.AddRange(transactionsContainer.TransactionList);
                    Console.WriteLine($"Retrieved {transactionsContainer.TransactionList.Count} transactions (Total so far: {allTransactions.Count})");
                }

                // Check if there are more pages
                hasMore = !string.IsNullOrEmpty(transactionsContainer.Next);
                offset += limit;

                // Safety check: if we've retrieved all transactions
                if (allTransactions.Count >= transactionsContainer.Total)
                {
                    hasMore = false;
                }
            }
            var look = from a in allTransactions where a.OrderId == "09-14052-99669" select a;
            Console.WriteLine($"Completed pagination. Total transactions retrieved: {allTransactions.Count}");
            return allTransactions;
        }

        private async Task<List<Order>> GetAllOrdersPaginated(string filter, int limit = 50)
        {
            var allOrders = new List<Order>();
            int offset = 0;
            bool hasMore = true;

            while (hasMore)
            {
                // Append offset to filter if not the first page
                string paginatedFilter = offset > 0 ? $"{filter},offset:{offset}" : filter;

                Orders ordersContainer = await _eBayController.GetOrders(
                    filter, limit, offset);

                if (ordersContainer.OrderList != null && ordersContainer.OrderList.Any())
                {
                    allOrders.AddRange(ordersContainer.OrderList);
                    Console.WriteLine($"Retrieved {ordersContainer.OrderList.Count} orders (Total so far: {allOrders.Count})");
                }

                // Check if there are more pages
                hasMore = !string.IsNullOrEmpty(ordersContainer.Next);
                offset += limit;

                // Safety check: if we've retrieved all orders
                if (allOrders.Count >= ordersContainer.Total)
                {
                    hasMore = false;
                }
            }

            Console.WriteLine($"Completed pagination. Total orders retrieved: {allOrders.Count}");
            return allOrders;
        }

        private async Task<bool> FormatToSendToGnuCash(List<Order> orders, List<Transaction> transactions, List<Payout> payouts)
        {
            try
            {
                // Instantiate converter with database connection manager
                var converter = new EbayToGnuCashConverter(_localDbConnectionManager);

                // Convert orders and transactions to GnuCash format
                Console.WriteLine($"Processing {orders.Count} orders, {transactions.Count} transactions, and {payouts.Count} payouts...");
                var gnuCashLines = converter.ConvertOrdersAndTransactions(orders, transactions, payouts, "Simmons_Ink");

                if (!gnuCashLines.Any())
                {
                    MessageBox.Show(
                        "No transactions were generated. This may indicate no PAYOUT transactions were found for the specified date range.",
                        "No Data to Export",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }

                // Create output path with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var outputPath = $@"D:\Exports\eBay_GnuCash_{timestamp}.csv";

                // Export to CSV
                CsvExporter.WriteToCsv(gnuCashLines, outputPath);

                // Show success message
                MessageBox.Show(
                    $"Successfully exported {gnuCashLines.Count} transaction lines to:\n\n{outputPath}\n\n" +
                    $"Orders processed: {orders.Count}\n" +
                    $"PAYOUT transactions found: {transactions.Count(t => t.TransactionStatus == TransactionStatusEnum.PAYOUT)}",
                    "Export Successful",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return true;
            }
            catch (Exception ex)
            {
                // Display user-friendly error message
                MessageBox.Show(
                    $"An error occurred while exporting to GnuCash:\n\n{ex.Message}\n\n" +
                    $"Please check the console output for detailed error information.",
                    "Export Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Console.WriteLine($"Error in FormatToSendToGnuCash: {ex}");
                return false;
            }
        }

        private async Task<SigningKey> GetOrCreateSigningKey(EbayController ebayController)
        {
            // 1. Try to get from database
            FeeBaySigningKeys? cachedKey = null;

            cachedKey = await _localDbConnectionManager.GetSigningKeyAsync();

            //// cachedKey = null; // Force create new key for testing
            if(cachedKey != null)
            {
                return cachedKey.ToSigningKey();
            }

            SigningKey key;

            {
                // 2. Create new if none exist
                key = await ebayController.CreateSigningKey();
            }

            // 3. Store in database
            await _localDbConnectionManager.SaveSigningKeyAsync(key.ToFeeBaySigningKey());

            return key;
        }
        public static string ToEbayDate(DateTime dateTime)
        {
            return dateTime.ToUniversalTime().ToString("o");
        }
        #endregion
        #endregion
    }
}
