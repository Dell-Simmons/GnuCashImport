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
        private readonly IOAuthTokenService _oAuthTokenService;
        private readonly ILocalDbConnectionManager _localDbConnectionManager;
        private readonly Func<string, EbayController> _ebayControllerFactory;

        public Form1(IOAuthTokenService oAuthTokenFactory, ILocalDbConnectionManager localDbConnectionManager, Func<string, EbayController> ebayControllerFactory)
        {
            InitializeComponent();
            _oAuthTokenService = oAuthTokenFactory;
            _localDbConnectionManager = localDbConnectionManager;
            _ebayControllerFactory = ebayControllerFactory;
           
        }
        //public Form1()
        //{
        //    InitializeComponent();
        //}
        private async void button1_Click(object sender, EventArgs e)
        {
            string? token = await _oAuthTokenService.GetOAuthTokenAsync("Simmons_Ink");
            var ebayController = _ebayControllerFactory(token);
            GetOrCreateSigningKey(ebayController, "Simmons_Ink").ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    var signingKey = task.Result;
                    MessageBox.Show($"Signing Key ID: {signingKey.SigningKeyId}");
                }
                else
                {
                    MessageBox.Show($"Error: {task.Exception?.GetBaseException().Message}");
                }
            });
         
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private async Task<SigningKey> GetOrCreateSigningKey(EbayController ebayController, string accountName)
        {
            // 1. Try to get from database
            FeeBaySigningKey? cachedKey = null;
            try
            {
                cachedKey = await _localDbConnectionManager.GetSigningKeyAsync();

            }
            catch (Exception f)
            {

                throw;
            }
            if (cachedKey != null)
                return cachedKey.ToSigningKey();

            // 2. Request from eBay
            var signingKeys = await ebayController.GetSigningKeys();
            SigningKey key;
           
            if (signingKeys?.SigningKeyList?.Length > 0)
            {
                key = signingKeys.SigningKeyList[0];
            }
            else
            {
                // 3. Create new if none exist
                key = await ebayController.CreateSigningKey();
            }

            // 4. Store in database
            await _localDbConnectionManager.SaveSigningKeyAsync(key.ToFeeBaySigningKey());

            return key;
        }
    }
}
