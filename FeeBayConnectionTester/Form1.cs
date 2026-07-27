using EbaySharp.Controllers;
using EbaySharp.Entities.Develop.ApplicationSettingsInsights.KeyManagement.SigningKey;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances.Payout;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances.Transaction;
using EbaySharp.Entities.Develop.SellingApps.OrderManagement.Fulfillment.Order;
using FeeBayConnectionTester.DTO;
using FeeBayConnectionTester.Extensions;
using FeeBayConnectionTester.Services;
using FeeBayConnectionTester.Services.FeeBay;
using FeeBayConnectionTester.Services.PeakCreditUnion;
using FeeBayConnectionTester.Services.SimpleFin;
using FeeBayConnectionTester.Services.Stripe;
using FeeBayOAuth.TokenService;
using LocalDBConnections;
using LocalDBConnections.StampDataDB.StampDataEntities;
using SimpleFin.SimpleFinDTO;
using System.Globalization;

namespace FeeBayConnectionTester
{
    public partial class Form1 : Form
    {
        #region Constants and Fields
        private readonly Func<string, EbayController> _ebayControllerFactory;
        private readonly IFeeBayTransactionProcessor _feeBayTransactionProcessor;
        private readonly ILocalDbConnectionManager _localDbConnectionManager;
        private readonly IOAuthTokenService _oAuthTokenService;
        private readonly SimpleFin.SimpleFinClient _simpleFinClient;
        private readonly ISparkCCTransactionProcessor _sparkCCTransactionProcessor;
        private readonly IPeakCUTransactionProcessor _peakCUTransactionProcessor;
        private readonly StripeCCProcessor.StripeCCProcessorClient _stripeCCProcessor;
        private readonly IStripeTransactionProcessor _stripeTransactionProcessor;
        private EbayController _eBayController = null!;
        #endregion

        #region Constructors
        // Constructors
        public Form1(
            IOAuthTokenService oAuthTokenFactory,
            ILocalDbConnectionManager localDbConnectionManager,
            Func<string, EbayController> ebayControllerFactory,
            SimpleFin.SimpleFinClient simpleFinClient,
            IFeeBayTransactionProcessor transactionProcessor,
            ISparkCCTransactionProcessor sparkCCTransactionProcessor,
            IPeakCUTransactionProcessor peakCUTransactionProcessor,
            StripeCCProcessor.StripeCCProcessorClient stripeCCProcessor,
            IStripeTransactionProcessor stripeTransactionProcessor)
        {
            InitializeComponent();
            _oAuthTokenService = oAuthTokenFactory;
            _localDbConnectionManager = localDbConnectionManager;
            _ebayControllerFactory = ebayControllerFactory;
            _simpleFinClient = simpleFinClient;
            _feeBayTransactionProcessor = transactionProcessor;
            _sparkCCTransactionProcessor = sparkCCTransactionProcessor;
            _peakCUTransactionProcessor = peakCUTransactionProcessor;
            _stripeCCProcessor = stripeCCProcessor;
            _stripeTransactionProcessor = stripeTransactionProcessor;
        }
        #endregion

        #region Event handlers
        #region
        private async void btnFeeBay_Click(object sender, EventArgs e) { await ProcessFeeBayTransactions(); }
        #endregion

        #region
        private async void btnPeakCU_Click(object sender, EventArgs e) { await ProcessPeakCUTransactions(); }
        #endregion

        #region
        private async void btnShippo_Click(object sender, EventArgs e) { await ProcessShippoTransactions(); }
        #endregion

        #region
        private async void btnSimpleFin_Click(object sender, EventArgs e) { await ProcessSparkCCTransactions(); }
        #endregion

        #region
        private async void btnStripe_Click(object sender, EventArgs e) { await ProcessStripeTransactions(); }
        #endregion

        #region
        private void button1_Click(object sender, EventArgs e) { throw new NotImplementedException(); }
        #endregion

        #region
        // Event handlers
        private void Form1_Load(object sender, EventArgs e)
        {
        }
        #endregion
        #endregion
        #region Methods
        #region Public Methods
        // Methods
        // Public Methods
        public static string ToEbayDate(DateTime dateTime) { return dateTime.ToUniversalTime().ToString("o"); }
        #endregion

        #region Private Methods
        // API Pagination
        private async Task<List<Order>> GetAllOrdersPaginated(string filter, int limit = 50)
        {
            var allOrders = new List<Order>();
            int offset = 0;
            bool hasMore = true;

            while(hasMore)
            {
                Orders ordersContainer = await _eBayController.GetOrders(filter, limit, offset);

                if(ordersContainer.OrderList != null && ordersContainer.OrderList.Any())
                {
                    allOrders.AddRange(ordersContainer.OrderList);
                    Console.WriteLine(
                        $"Retrieved {ordersContainer.OrderList.Count} orders (Total so far: {allOrders.Count})");
                }

                hasMore = !string.IsNullOrEmpty(ordersContainer.Next);
                offset += limit;

                if(allOrders.Count >= ordersContainer.Total)
                {
                    hasMore = false;
                }
            }

            Console.WriteLine($"Completed pagination. Total orders retrieved: {allOrders.Count}");
            return allOrders;
        }

        private async Task<List<Payout>> GetAllPayOutsPaginated(string filter, int limit = 50)
        {
            var allPayouts = new List<Payout>();
            int offset = 0;
            bool hasMore = true;

            while(hasMore)
            {
                PayoutList payoutsContainer = await _eBayController.GetPayouts(filter, null, limit, offset);

                if(payoutsContainer.Payouts != null && payoutsContainer.Payouts.Any())
                {
                    allPayouts.AddRange(payoutsContainer.Payouts);
                    Console.WriteLine(
                        $"Retrieved {payoutsContainer.Payouts.Count} payouts (Total so far: {allPayouts.Count})");
                }

                hasMore = !string.IsNullOrEmpty(payoutsContainer.Next);
                offset += limit;

                if(allPayouts.Count >= payoutsContainer.Total)
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

            while(hasMore)
            {
                Transactions transactionsContainer = await _eBayController.GetTransactions(filter, null, limit, offset);

                if(transactionsContainer.TransactionList != null && transactionsContainer.TransactionList.Any())
                {
                    allTransactions.AddRange(transactionsContainer.TransactionList);
                    Console.WriteLine(
                        $"Retrieved {transactionsContainer.TransactionList.Count} transactions (Total so far: {allTransactions.Count})");
                }

                hasMore = !string.IsNullOrEmpty(transactionsContainer.Next);
                offset += limit;

                if(allTransactions.Count >= transactionsContainer.Total)
                {
                    hasMore = false;
                }
            }

            Console.WriteLine($"Completed pagination. Total transactions retrieved: {allTransactions.Count}");
            return allTransactions;
        }

        private async Task<SigningKey> GetOrCreateSigningKey(EbayController ebayController)
        {
            // Try to get from database
            FeeBaySigningKeys? cachedKey = await _localDbConnectionManager.GetSigningKeyAsync();

            if(cachedKey != null)
            {
                return cachedKey.ToSigningKey();
            }

            // Create new if none exist
            SigningKey key = await ebayController.CreateSigningKey();

            // Store in database
            await _localDbConnectionManager.SaveSigningKeyAsync(key.ToFeeBaySigningKey());

            return key;
        }

        private async Task<SimpleFinAccessTokens?> GetSimpleFinAccessToken()
        {
            var simpleFinAccessToken = await _localDbConnectionManager.GetSimpleFinAccessToken("Peak CU");

            if(simpleFinAccessToken == null)
            {
                using var dlg = new FormAskForSimpleFinSetupToken();
                if(dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string setupToken = dlg.SetupToken;
                    var accessToken = await SimpleFin.SimpleFinClient.Connect3(setupToken);
                    simpleFinAccessToken = new SimpleFinAccessTokens();
                    simpleFinAccessToken.BankName = "Peak CU";
                    simpleFinAccessToken.AccessToken = accessToken;
                }

                if(simpleFinAccessToken == null)
                {
                    throw new NotImplementedException();
                }

                _ = await _localDbConnectionManager.SaveSimpleFinAccessToken(simpleFinAccessToken);
                return await GetSimpleFinAccessToken();
            }

            return simpleFinAccessToken;
        }

        private async Task ProcessFeeBayTransactions()
        {
            (List<Payout> payoutList, List<Transaction> transactionList, List<Order> orderList)
                = await PullFeeBayTransactions();

            List<ToGnuCash> feeBayIncomingData = await _feeBayTransactionProcessor.ProcessTransactionsAsync(
                payoutList,
                transactionList,
                orderList);

            var incomingTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var incomingOutputPath = $@"D:\Exports\eBay_IncomingData_{incomingTimestamp}.csv";

            // Sort by date for testing
            feeBayIncomingData = feeBayIncomingData
                .OrderBy(d => d.Date)
                .ToList();

            CsvExporter.WriteIncomingDataToCsv(feeBayIncomingData, incomingOutputPath);
            MessageBox.Show(
                $"Successfully exported {feeBayIncomingData.Count} incoming rows to:\n\n{incomingOutputPath}",
                "Incoming Data Export Successful",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

     

        private async Task ProcessShippoTransactions()
        {
            throw new NotImplementedException();
            // really no need to do this since we are not using Shippo for anything other than shipping labels,
            // but if you want to pull the data from Shippo, you can use the following code:   
            // var shippoAccessToken = await _localDbConnectionManager.GetSimpleFinAccessToken("Shippo_Token");
            // string shippoSecretKey = shippoAccessToken.AccessToken;
            // _shippoMailingLabelClient.PullShippoMailingLabelPurchases(shippoSecretKey);
        }

        private async Task ProcessPeakCUTransactions()
        {
            string businessCheckingId = "ACT-889f0bab-1c17-4dba-b790-21c3facbc0e6";
            SimpleFinAccessTokens? simpleFinAccessToken = await GetSimpleFinAccessToken();
            if(simpleFinAccessToken == null)
            {
                MessageBox.Show(
                    "Unable to acquire SimpleFIN access token for Peak CU.",
                    "Authentication Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            AccountResponse peakCUTransactions= 
                await SimpleFin.SimpleFinClient.FetchAccountDataAsync(
                    simpleFinAccessToken.AccessToken, businessCheckingId);
            List<ToGnuCash> peakCUIncomingData =
            await _peakCUTransactionProcessor.ProcessPeakCuTransactionsAsync(
                peakCUTransactions.Accounts.First().Transactions);

            var incomingTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var incomingPeakCUOutputPath = $@"D:\Exports\PeakCU_IncomingData_{incomingTimestamp}.csv";
             // Sort by date for testing
            peakCUIncomingData = peakCUIncomingData
                .OrderBy(d => d.Date)
                .ToList();
           
            CsvExporter.WriteIncomingDataToCsv(peakCUIncomingData, incomingPeakCUOutputPath);
            MessageBox.Show(
               $"Successfully exported {peakCUIncomingData.Count} incoming rows to:\n\n{incomingPeakCUOutputPath}",
               "Incoming Data Export Successful",
               MessageBoxButtons.OK,
               MessageBoxIcon.Information);
        }

        private async Task ProcessSparkCCTransactions()
        {
                  string sparkBusinessCC = "ACT-fe0b66ca-58d4-40fe-8620-7352f97ebcca";
      
            SimpleFinAccessTokens? simpleFinAccessToken = await GetSimpleFinAccessToken();
            
            AccountResponse sparkCCTransactions = await SimpleFin.SimpleFinClient.FetchAccountDataAsync(
                simpleFinAccessToken.AccessToken, sparkBusinessCC);
           

            List<ToGnuCash> sparkCCIncomingData =
            await _sparkCCTransactionProcessor.ProcessSparkCCTransactionsAsync(
                sparkCCTransactions.Accounts.First().Transactions);

         
            var incomingTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var incomingSparkCCOutputPath = $@"D:\Exports\SparkCC_IncomingData_{incomingTimestamp}.csv";
         
            // Sort by date for testing
            sparkCCIncomingData = sparkCCIncomingData
                .OrderBy(d => d.Date)
                .ToList();

            CsvExporter.WriteIncomingDataToCsv(sparkCCIncomingData, incomingSparkCCOutputPath);
         
            MessageBox.Show(
               $"Successfully exported {sparkCCIncomingData.Count} incoming rows to:\n\n{incomingSparkCCOutputPath}",
               "Incoming Data Export Successful",
               MessageBoxButtons.OK,
               MessageBoxIcon.Information);
        }

        private async Task ProcessStripeTransactions()
        {
            // string stripeSecretKey = "sk_live_25nKeitLKW2tgf6CTLDJWoNc";
            var stripeAccessToken = await _localDbConnectionManager.GetSimpleFinAccessToken("Stripe_Secret");
            string stripeSecretKey = stripeAccessToken.AccessToken;
            DateTime startDate = DateTime.Now.AddMonths(-8);
            DateTime endDate = DateTime.Now; // Assign your Stripe secret key here
            var dsdSales = await _stripeCCProcessor.PullSalesAsync(startDate, endDate, stripeSecretKey);

            List<ToGnuCash> stripeIncomingData = await _stripeTransactionProcessor.ReformatStripeForGnuCashAsync(
                dsdSales);
            var incomingTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var incomingOutputPath = $@"D:\Exports\stripe_IncomingData_{incomingTimestamp}.csv";

            // Sort by date for testing
            //stripeIncomingData = stripeIncomingData
            //    .OrderBy(d => d.Date)
            //    .ToList();

            CsvExporter.WriteIncomingDataToCsv(stripeIncomingData, incomingOutputPath);
            MessageBox.Show(
                $"Successfully exported {stripeIncomingData.Count} incoming rows to:\n\n{incomingOutputPath}",
                "Incoming Data Export Successful",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // eBay Data Retrieval
        private async Task<(List<Payout>, List<Transaction>, List<Order>)> PullFeeBayTransactions()
        {
            string? token = await _oAuthTokenService.GetOAuthTokenAsync("Simmons_Ink");
            if(string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show(
                    "Unable to acquire an OAuth token for Simmons_Ink.",
                    "Authentication Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return (new List<Payout>(), new List<Transaction>(), new List<Order>());
            }

            _eBayController = _ebayControllerFactory(token);
            var signingKey = await GetOrCreateSigningKey(_eBayController);

            // Define date filters
            string payOutsFilter = "payoutDate:[2026-06-01T00:00:00.000Z..2026-07-14T23:59:59.999Z]";
            string transactionsFilter = "transactionDate:[2025-05-25T00:00:00.000Z..2026-07-14T23:59:59.000Z]";
            string ordersFilter = "creationdate:[2025-05-25T00:00:00.000Z..2026-07-14T23:59:59.999Z]";

            // Fetch all data
            var payoutList = await GetAllPayOutsPaginated(payOutsFilter, limit: 50);
            var transactionList = await GetAllTransactionsPaginated(transactionsFilter, limit: 50);
            var orderList = await GetAllOrdersPaginated(ordersFilter, limit: 50);

            return (payoutList, transactionList, orderList);
        }

        private async Task<AccountResponse> PullSimpleFinTransactions()
        {
            SimpleFinAccessTokens? simpleFinAccessToken = await GetSimpleFinAccessToken();
            return await SimpleFin.SimpleFinClient.FetchAccountDataAsync(simpleFinAccessToken.AccessToken,string.Empty);
        }
        #endregion
        #endregion
    }
}
