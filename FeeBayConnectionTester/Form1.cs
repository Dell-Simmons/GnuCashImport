
using EbaySharp.Controllers;
using FeeBayOAuth.TokenService;

namespace FeeBayConnectionTester
{
    public partial class Form1 : Form
    {
        private readonly IOAuthTokenService _oAuthTokenFactory;
        private readonly EbayController _ebayController;

        public Form1(IOAuthTokenService oAuthTokenFactory, EbayController ebayController)
        {
            InitializeComponent();
            _oAuthTokenFactory = oAuthTokenFactory;
            _ebayController = ebayController;
            // _ebayFinancesClient = ebayFinancesClient;
        }
        //public Form1()
        //{
        //    InitializeComponent();
        //}
        private async void button1_Click(object sender, EventArgs e)
        {
            string? token = await _oAuthTokenFactory.GetOAuthTokenAsync("Simmons_Ink");
            // SellerFundsSummaryResponse sellerFundsSummaryResponse = await _ebayFinancesClient.GetSellerFundsSummaryAsync();
     
            MessageBox.Show(token);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
