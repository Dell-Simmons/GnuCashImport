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

        private void button1_Click(object sender, EventArgs e)
        {
            string token = _oAuthTokenFactory.GetOAuthToken("Simmons_Ink");
            MessageBox.Show(token);
        }
    }
}
