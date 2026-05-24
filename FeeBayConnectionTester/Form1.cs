using FeeBayOAuth.TokenFactory;
using LocalDBConnections;

namespace FeeBayConnectionTester
{
    public partial class Form1 : Form
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILocalDbConnectionManager _localDbConnectionManager;

        public Form1(
            IHttpClientFactory httpClientFactory,
            ILocalDbConnectionManager localDbConnectionManager)
        {
            InitializeComponent();
            _httpClientFactory = httpClientFactory;
            _localDbConnectionManager = localDbConnectionManager;
        }

        private void button1_Click(object sender, EventArgs e)
        {

            var feeBayOAuthTokenFactory = new OAuthTokenFactory(_httpClientFactory, _localDbConnectionManager);
            //{
            //    string token = feeBayOAuthTokenFactory.GetOAuthToken("Simmons_Ink");
            //    MessageBox.Show(token);
            //}
        }
    }
}
