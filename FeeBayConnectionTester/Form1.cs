using EbaySharp.Controllers;
using EbaySharp.Entities.Develop.KeyManagement.SigningKey;
using FeeBayConnectionTester.Extensions;
using FeeBayOAuth.TokenService;
using LocalDBConnections;
using LocalDBConnections.StampDataDB.StampDataEntities;

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
        #region
        //public Form1()
        //{
        //    InitializeComponent();
        //}
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
            var signingKey = await GetOrCreateSigningKey(ebayController);

            var asdf = await ebayController.GetTransactionSummary(signingKey, "transactionType:{SALE}");
        }
        #endregion

        #region
        private void Form1_Load(object sender, EventArgs e)
        {
        }
        #endregion
        #endregion
        #region Methods
        #region Private Methods
        private async Task<SigningKey> GetOrCreateSigningKey(EbayController ebayController)
        {
           // // 1. Try to get from database
            FeeBaySigningKeys? cachedKey = null;

            cachedKey = await _localDbConnectionManager.GetSigningKeyAsync();

           //// cachedKey = null; // Force create new key for testing
            if (cachedKey != null)
            {
                return cachedKey.ToSigningKey();
            }

           // // 2. Request from eBay
           // var signingKeys = await ebayController.GetSigningKeys();
            SigningKey key;

           // if(signingKeys?.SigningKeyList?.Length > 0)
           // {
           //     key = signingKeys.SigningKeyList[0];
           // } else
            {
                // 3. Create new if none exist
                key = await ebayController.CreateSigningKey();
            }

            // 4. Store in database
            await _localDbConnectionManager.SaveSigningKeyAsync(key.ToFeeBaySigningKey());

            return key;
        }
        #endregion
        #endregion
    }
}
