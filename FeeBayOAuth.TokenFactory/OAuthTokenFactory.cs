using FeeBayOAuth.TokenFactory.Calls;
using FeeBayOAuth.TokenFactory.DTO;
using FeeBayOAuth.TokenFactory.DTO.Response;
using LocalDBConnections;
using Microsoft.Extensions.Configuration;
using System;

namespace FeeBayOAuth.TokenFactory
{
    public class OAuthTokenFactory
    {
        #region Constructors
        public OAuthTokenFactory(
            IHttpClientFactory httpClientFactory,
            ILocalDbConnectionManager localDBConnectionManager)
        {
            _httpClientFactory = httpClientFactory;
            _localDbConnectionManager = localDBConnectionManager;

            string clientId = GetClientIdFromAppSettings();
            string clientSecret = GetClientSecretFromAppSettings();
            _userTokenService = new UserTokenService(httpClientFactory, clientId, clientSecret);
        }
        #endregion

        #region Methods
        #region Public Methods
        public async Task<string?> GetOAuthTokenAsync(string feeBayUserName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(feeBayUserName))
            {
                return null;
            }

            // Check if the user token is in the dictionary and not expired or expiring soon. If so return it.
            var foundToken = _oAuthTokensDictionary.TryGetValue(feeBayUserName, out UserToken? userToken);
            if (foundToken && userToken?.IsValid == true && userToken?.ExpiresSoon == false)
            {
                return userToken.AccessToken;
            }

            // If the user token is not in the dictionary or is expired or expiring soon, get the user token from the database and check if it is expired or expiring soon.
            // If it is not expired or expiring soon, return it. If it is expired or expiring soon, use the refresh token to get a new user token.       
            string? refreshToken = GetRefreshTokenFromDataBase(feeBayUserName);
            if (string.IsNullOrEmpty(refreshToken))
            {
                return null;
            }

            var result = await _userTokenService.GetUserTokenAsync(refreshToken, cancellationToken);

            if (!result.IsSuccess || result.Response == null)
            {
                HandleGetUserTokenError();
                return null;
            }

            // TryAdd does nothing if the key value pair is already in the dictionary. Need for the second pass used for expired token
            _oAuthTokensDictionary.TryAdd(feeBayUserName, new UserToken
            {
                AccessToken = result.Response.access_token,
                ExpiresUtc = DateTime.UtcNow.AddSeconds(result.Response.expires_in)
            });

            foundToken = _oAuthTokensDictionary.TryGetValue(feeBayUserName, out UserToken? token);
            if (foundToken && token != null)
            {
                return token.AccessToken;
            }

            return string.Empty;
        }

        public void Reset(string feeBayUser) => _oAuthTokensDictionary.Remove(feeBayUser);
        #endregion

        #region Private Methods
        private string GetClientIdFromAppSettings()
        {
            string appId;
            appId = "SimmonsI-7ec9-4395-8fdd-0de9eae5ef72";// _configuration.GetValue<string>("FeeBayOAuthConnection:appid");
            return appId;
        }

        private string GetClientSecretFromAppSettings()
        {
            string certId;
            certId = "fdf79779-7f0d-4bce-9c62-33ba709c43b0";// _configuration.GetValue<string>("FeeBayOAuthConnection:certid");
            return certId;
        }

        private string GetRefreshTokenFromDataBase(string feeBayUser)
        {
            try
            {
                string refreshToken;
                refreshToken = _localDbConnectionManager.GetRefreshToken(feeBayUser);
                return refreshToken;
            } catch(Exception ex)
            {
                throw;
            }
        }

    

        private void HandleGetUserTokenError() => throw new NotImplementedException();

      

        private bool UserIsInDictionary(string feeBayUser)
        {
            var userExists = _oAuthTokensDictionary.ContainsKey(feeBayUser);
            return userExists;
        }

        private bool UserTokenHasExpiredOrWillExpireSoon(DateTime userTokenExpireTime)
        {
            if(userTokenExpireTime < DateTime.Now.Add(new TimeSpan(0, 10, 0)).ToLocalTime())
            {
                return true;
            }
            return false;
        }
        #endregion
        #endregion
        #region Fields
        private Dictionary<string, UserToken> _oAuthTokensDictionary = new Dictionary<string, UserToken>();
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILocalDbConnectionManager _localDbConnectionManager;
        private readonly UserTokenService _userTokenService;
        #endregion Fields
    }
}
