using FeeBayOAuth.TokenFactory.DTO.Response;
using FeeBayOAuth.TokenFactory.Utilities;
using Newtonsoft.Json;
using System.Text;

namespace FeeBayOAuth.TokenFactory.Calls
{
    public static class Get_User_Token
    {
        public static Get_UserToken_Response MakeCall(string refreshToken,
                        IHttpClientFactory _httpClientFactory,
                        string appId,
                        string certId, out Response_Errors errorsContainer)
        {
            var _refreshToken = refreshToken;
            var _appId = appId;
            var _certId = certId;
            var scopes = SetUserScopes();
            var formattedScopes = FormatScopesForRequest(scopes);
            var payloadParams = new Dictionary<string, string>
                {
                    {"grant_type", "refresh_token" },
                    {"refresh_token", _refreshToken}//,
                   // {"scope", formattedScopes}
                };
            var requestPayload = OAuthHttpHelpers.CreateRequestPayload(payloadParams);
            var myClient = _httpClientFactory.CreateClient();

            myClient.BaseAddress = new Uri(@"https://api.ebay.com/identity/v1/oauth2/token");
            HttpRequestMessage myRequest = new HttpRequestMessage(HttpMethod.Post, "");
            myClient.DefaultRequestHeaders.Add("Authorization", OAuthHttpHelpers.CreateAuthorizationHeader(_appId, _certId));

            HttpContent content = new StringContent(requestPayload, Encoding.UTF8, "application/x-www-form-urlencoded");
            myRequest.Content = content;
            HttpResponseMessage response = myClient.SendAsync(myRequest).Result;

            //errorsContainer = null;
            if (response.IsSuccessStatusCode)
            {
                var responseContent = response.Content.ReadAsStringAsync().Result;
                Get_UserToken_Response userTokenContainer = JsonConvert.DeserializeObject<Get_UserToken_Response>(responseContent);
                return userTokenContainer;
            }
            else
            {
                var returnString = response.Content.ReadAsStringAsync().Result;
                var errorResponse =
                  JsonConvert.DeserializeObject<Response_Errors>(response.Content.ReadAsStringAsync().Result);
                errorsContainer = errorResponse;
            }
            return null;
        }

        private static IList<string> SetUserScopes()
        {
            IList<string> userScopes = new List<String>()
            {
                "https://api.ebay.com/oauth/api_scope",
                "https://api.ebay.com/oauth/api_scope/sell.marketing",
                "https://api.ebay.com/oauth/api_scope/sell.inventory",
                "https://api.ebay.com/oauth/api_scope/sell.account",
                "https://api.ebay.com/oauth/api_scope/sell.fulfillment",
                "https://api.ebay.com/oauth/api_scope/sell.analytics.readonly",
                "https://api.ebay.com/oauth/api_scope/sell.stores"
            };
            return userScopes;
        }

        private static String FormatScopesForRequest(IList<String> scopes)
        {
            String scopesForRequest = null;
            if (scopes == null || scopes.Count == 0)
            {
                return scopesForRequest;
            }

            foreach (String scope in scopes)
            {
                scopesForRequest = scopesForRequest == null ? scope : scopesForRequest + " " + scope;
            }
            return Uri.EscapeDataString(scopesForRequest);
        }
    }
}
