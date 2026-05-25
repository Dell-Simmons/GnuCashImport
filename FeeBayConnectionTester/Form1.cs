using FeeBayOAuth.TokenFactory;

namespace FeeBayConnectionTester
{
    public partial class Form1 : Form
    {
        private readonly OAuthTokenFactory _oAuthTokenFactory;

        public Form1(OAuthTokenFactory oAuthTokenFactory)
        {
            InitializeComponent();
            _oAuthTokenFactory = oAuthTokenFactory;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string? token = await _oAuthTokenFactory.GetOAuthTokenAsync("Simmons_Ink");
            MessageBox.Show(token ?? "No token available");
        }
    }
}
