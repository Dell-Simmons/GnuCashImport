using FeeBayFinances;
using FeeBayFinances.Calls;
using FeeBayOAuth.TokenFactory;

namespace FeeBayConnectionTester
{
    public partial class Form1 : Form
    {
     //   private readonly OAuthTokenService _oAuthTokenFactory;
        private readonly EbayFinancesClient _ebayFinancesClient;

        public Form1(EbayFinancesClient ebayFinancesClient)
        {
            InitializeComponent();
          //  _oAuthTokenFactory = oAuthTokenFactory;
            _ebayFinancesClient = ebayFinancesClient;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
         //   string? token = await _oAuthTokenFactory.GetOAuthTokenAsync("Simmons_Ink");
            SellerFundsSummaryResponse sellerFundsSummaryResponse = await _ebayFinancesClient.GetSellerFundsSummaryAsync();
            MessageBox.Show(sellerFundsSummaryResponse.AvailableFunds?.Value);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
