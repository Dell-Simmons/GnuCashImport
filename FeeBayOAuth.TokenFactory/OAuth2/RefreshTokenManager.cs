using FeeBayOAuth.TokenFactory.Models;
using FeeBayOAuth.TokenFactory.Utilities;
using LocalDBConnections;
using LocalDBConnections.StampDataDB.StampdataEntities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace FeeBayOAuth.TokenFactory.OAuth2
{
    /// <summary>
    /// Manages the initial OAuth authorization flow to obtain refresh tokens from eBay
    /// </summary>
    public class RefreshTokenManager
    {
        #region Fields

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILocalDbConnectionManager _localDbConnectionManager;

        #endregion

        #region Constructors

        public RefreshTokenManager(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILocalDbConnectionManager localDbConnectionManager)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _localDbConnectionManager = localDbConnectionManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Exchanges an authorization code for a new refresh token and access token
        /// </summary>
        /// <param name="userAuthCode">Authorization code received from eBay's consent flow</param>
        /// <param name="feeBayUserId">eBay user ID to associate with these tokens</param>
        /// <returns>True if tokens were successfully retrieved and saved</returns>
        public bool GetNewRefreshTokenFromFeeBay(string userAuthCode, string feeBayUserId)
        {
            // Get configuration values
            var redirectUri = _configuration.GetValue<string>("FeeBayOAuthConnection:redirectUri");
            var appId = _configuration.GetValue<string>("FeeBayOAuthConnection:appid");
            var certId = _configuration.GetValue<string>("FeeBayOAuthConnection:certid");

            // Validate inputs
            if (string.IsNullOrEmpty(userAuthCode) || string.IsNullOrEmpty(feeBayUserId))
            {
                return false;
            }

            if (string.IsNullOrEmpty(redirectUri) || string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(certId))
            {
                return false;
            }

            // Create the HTTP client
            var myClient = _httpClientFactory.CreateClient();
            myClient.BaseAddress = new Uri(@"https://api.ebay.com");

            // Add headers
            myClient.DefaultRequestHeaders.Clear();
            myClient.DefaultRequestHeaders.Add("Authorization", OAuthHttpHelpers.CreateAuthorizationHeader(appId, certId));

            // Create request payload for authorization code grant
            Dictionary<string, string> payloadParams = new Dictionary<string, string>
            {
                {"grant_type", "authorization_code" },
                {"redirect_uri", redirectUri},
                {"code", userAuthCode}
            };

            string requestPayload = OAuthHttpHelpers.CreateRequestPayload(payloadParams);
            HttpContent content = new StringContent(requestPayload, Encoding.UTF8, "application/x-www-form-urlencoded");

            // Make the request to eBay's OAuth token endpoint
            var response = myClient.PostAsync("identity/v1/oauth2/token", content).Result;
            var returnString = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                // Deserialize the response
                OAuthApiResponse apiResponse = JsonConvert.DeserializeObject<OAuthApiResponse>(returnString);

                // Create the tokens entity
                FeeBayOAuthTokens feeBayOAuthTokens = new FeeBayOAuthTokens
                {
                    FeeBayUserName = feeBayUserId,
                    OAuthToken = apiResponse.AccessToken,
                    OAuthTokenExpire = DateTime.Now.Add(new TimeSpan(0, 0, apiResponse.ExpiresIn)),
                    RefreshToken = apiResponse.RefreshToken,
                    RefreshTokenExpire = DateTime.Now.Add(new TimeSpan(0, 0, apiResponse.RefreshTokenExpiresIn))
                };

                // Save to database
                return _localDbConnectionManager.SaveUserToken(feeBayOAuthTokens);
            }

            return false;
        }

        #endregion
    }
}
