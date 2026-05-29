using EbaySharp.Controllers;
using EbaySharp.Entities.Develop.KeyManagement.SigningKey;
using FeeBayOAuth.TokenService;

namespace FeeBayConnectionTester
{
    public partial class Form1 : Form
    {
        private readonly IOAuthTokenService _oAuthTokenService;
        private readonly Func<string, EbayController> _ebayControllerFactory;

        public Form1(IOAuthTokenService oAuthTokenFactory, Func<string, EbayController> ebayControllerFactory)
        {
            InitializeComponent();
            _oAuthTokenService = oAuthTokenFactory;
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
    
            SigningKeys signingKeys = await ebayController.GetSigningKeys();
    
            if (signingKeys?.SigningKeyList == null ||signingKeys.SigningKeyList.Length == 0)
            {
                MessageBox.Show("No signing keys found. Creating one...");
                var newKey = await ebayController.CreateSigningKey();
                MessageBox.Show($"Created signing key: {newKey.SigningKeyId}");
            }
            else
            {
                MessageBox.Show($"Found {signingKeys.SigningKeyList.Length} signing key(s)");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
