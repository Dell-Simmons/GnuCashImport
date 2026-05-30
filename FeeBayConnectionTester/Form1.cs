using EbaySharp.Controllers;
using EbaySharp.Entities.Common;
using EbaySharp.Entities.Develop.KeyManagement.SigningKey;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances;
using EbaySharp.Entities.Develop.SellingApps.AccountManagement.Finances.Transaction;
using EbaySharp.Entities.Develop.SellingApps.OrderManagement.Fulfillment.Order;
using FeeBayConnectionTester.Extensions;
using FeeBayOAuth.TokenService;
using LocalDBConnections;
using LocalDBConnections.StampDataDB.StampDataEntities;
using System;
using System.Linq;
using System.Collections.Generic;   

namespace FeeBayConnectionTester
{
    public partial class Form1 : Form
    {
        #region Constants and Fields
        private readonly Func<string, EbayController> _ebayControllerFactory;
        private readonly ILocalDbConnectionManager _localDbConnectionManager;
        private readonly IOAuthTokenService _oAuthTokenService;
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

            var ebayController = _ebayControllerFactory(token);
            // The signing key is used to create digital signatures for API requests,
            // it is associated with the application
            // and is used to ensure the integrity and authenticity of the requests.
            // The one stored in the database is good for 3 years from today (5/29/26).
            // So don't fucking worry about it expiring anytime soon.
            var signingKey = await GetOrCreateSigningKey(ebayController);

            string multiFilter;

            // Combine multiple filters into ONE comma-separated string

            //!{PAYOUT} is funds going from feeBay to bank account
            multiFilter = "transactionStatus:{PAYOUT},transactionDate:[2026-01-01T00:00:00.000Z..2026-01-31T23:59:59.000Z]";

            //TransactionSummary transactionPayoutSummary = 
            //    await ebayController.GetTransactionSummary(signingKey, multiFilter);
            ////!{ COMPLETED} is funds going from buyer to feeBay.
            //multiFilter = "transactionStatus:{COMPLETED},transactionDate:[2026-01-01T00:00:00.000Z..2026-01-31T23:59:59.000Z]";

            //TransactionSummary transactionCompletedSummary =
            //    await ebayController.GetTransactionSummary(signingKey, multiFilter);

            //!GetTransactions
            multiFilter = "transactionDate:[2026-01-01T00:00:00.000Z..2026-01-31T23:59:59.000Z]";
            Transactions transactions = await ebayController.GetTransactions(signingKey, multiFilter);
            List<Transaction> transactionList = transactions.TransactionList;
            FormatToSendToGnuCash(transactionList);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }
        #endregion
        #region Methods
        #region Private Methods
        private void FormatToSendToGnuCash(IEnumerable<Transaction> transactions)
        {
            //! Sort into Transaction Types
            var sales = transactions.Where(x => x.TransactionType == TransactionTypeEnum.SALE);
            var refund = transactions.Where(x => x.TransactionType == TransactionTypeEnum.REFUND);
            var credit = transactions.Where(x => x.TransactionType == TransactionTypeEnum.CREDIT);
            var dispute = transactions.Where(x => x.TransactionType == TransactionTypeEnum.DISPUTE);
            var shippingLabel = transactions.Where(x => x.TransactionType == TransactionTypeEnum.SHIPPING_LABEL);
            var transfer = transactions.Where(x => x.TransactionType == TransactionTypeEnum.TRANSFER);
            var nonSaleCharge = transactions.Where(x => x.TransactionType == TransactionTypeEnum.NON_SALE_CHARGE);
            var adjustment = transactions.Where(x => x.TransactionType == TransactionTypeEnum.ADJUSTMENT);
            var withdrawal = transactions.Where(x => x.TransactionType == TransactionTypeEnum.WITHDRAWAL);
            var loanRepayment = transactions.Where(x => x.TransactionType == TransactionTypeEnum.LOAN_REPAYMENT);
            var purchase = transactions.Where(x => x.TransactionType == TransactionTypeEnum.PURCHASE);

            var totalTransactions = transactions.Count();
            var salesTransactions = sales.Count();
            var refundTransactions = refund.Count();
            var creditTransactions = credit.Count();
            var disputeTransactions = dispute.Count();
            var shippingLabelTransactions = shippingLabel.Count();
            var transferTransactions = transfer.Count();
            var nonSaleChargeTransactions = nonSaleCharge.Count();
            var adjustmentTransactions = adjustment.Count();
            var withdrawalTransactions = withdrawal.Count();
            var loanRepaymentTransactions = loanRepayment.Count();
            var purchaseTransactions = purchase.Count();

            //! Sort into Transaction Status
            var onHold = transactions.Where(x => x.TransactionStatus == TransactionStatusEnum.FUNDS_ON_HOLD);
            var processing = transactions.Where(x => x.TransactionStatus == TransactionStatusEnum.FUNDS_PROCESSING);
            var availableForPayout = transactions.Where(x => x.TransactionStatus == TransactionStatusEnum.FUNDS_AVAILABLE_FOR_PAYOUT);
            var payout = transactions.Where(x => x.TransactionStatus == TransactionStatusEnum.PAYOUT);
            var completed = transactions.Where(x => x.TransactionStatus == TransactionStatusEnum.COMPLETED);
            var failed = transactions.Where(x => x.TransactionStatus == TransactionStatusEnum.FAILED);

            var onHoldTransactions = onHold.Count();
            var processingTransactions = processing.Count();
            var availableForPayoutTransactions = availableForPayout.Count();
            var payoutTransactions = payout.Count();
            var completedTransactions = completed.Count();
            var failedTransactions = failed.Count();
        }
        //FUNDS_ON_HOLD,
        //FUNDS_PROCESSING,
        //FUNDS_AVAILABLE_FOR_PAYOUT,
        //PAYOUT,
        //COMPLETED,
        //FAILED

        //SALE,
        //REFUND,
        //CREDIT,
        //DISPUTE,
        //SHIPPING_LABEL,
        //TRANSFER,
        //NON_SALE_CHARGE,
        //ADJUSTMENT,
        //WITHDRAWAL,
        //LOAN_REPAYMENT,
        //PURCHASE
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
