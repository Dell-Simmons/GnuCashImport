using EbaySharp.Controllers;
using FeeBayOAuth.TokenService;

namespace FeeBayConnectionTester
{
    public partial class Form1 : Form
    {
        private readonly IOAuthTokenService _oAuthTokenFactory;
        private readonly Func<string, EbayController> _ebayControllerFactory;

        public Form1(IOAuthTokenService oAuthTokenFactory, Func<string, EbayController> ebayControllerFactory)
        {
            InitializeComponent();
            _oAuthTokenFactory = oAuthTokenFactory;
            _ebayControllerFactory = ebayControllerFactory;
            // _ebayFinancesClient = ebayFinancesClient;
        }
        //public Form1()
        //{
        //    InitializeComponent();
        //}
        private async void button1_Click(object sender, EventArgs e)
        {
            string? token = await _oAuthTokenFactory.GetOAuthTokenAsync("Simmons_Ink");
            var ebayController = _ebayControllerFactory(token);
            // use ebayController...
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
