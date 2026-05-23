using FeeBayOAuth.TokenFactory.Calls;
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
            IConfiguration configuration,
            ILocalDbConnectionManager localDBConnectionManager)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _localDbConnectionManager = localDBConnectionManager;
        }
        #endregion

        #region Methods
        #region Public Methods
        public string GetOAuthToken(string feeBayUserName)
        {
            string _feeBayUser = feeBayUserName;
            Get_UserToken_Response oAuthUserTokenResponse = new Get_UserToken_Response();
            Response_Errors errorsContainer = null;
            if(string.IsNullOrEmpty(_feeBayUser))
            {
                return null;
            }
            if(!UserIsInDictionary(_feeBayUser))
            {
                string refreshToken = GetRefreshTokenFromDataBase(_feeBayUser);
                string clientId = GetClientIdFromAppSettings();
                string clientSecret = GetClientSecretFromAppSettings();
                string userToken = GetUserTokenFromDataBase(_feeBayUser);
                DateTime userTokenExpireTime = GetUserTokenExpireTimeFromDataBase(_feeBayUser);
                if(UserTokenHasExpiredOrWillExpireSoon(userTokenExpireTime))
                {
                    if(string.IsNullOrEmpty(refreshToken))
                    {
                        return null;
                    }
                    oAuthUserTokenResponse = Get_User_Token.MakeCall(
                        refreshToken,
                        _httpClientFactory,
                        clientId,
                        clientSecret,
                        out errorsContainer);
                    SaveUserTokenToDatabase(oAuthUserTokenResponse, _feeBayUser);

                    GetOAuthToken(_feeBayUser);
                } else
                {
                    oAuthUserTokenResponse.access_token = userToken;
                }

                if(oAuthUserTokenResponse == null && errorsContainer != null)
                {
                    HandleGetUserTokenError();
                    return null;
                }
                // TryAdd does nothing if the key value pair is already in the dictionary.  Need for the second pass used for expired token
                _oAuthTokensDictionary.TryAdd(_feeBayUser, oAuthUserTokenResponse.access_token);
            }
            var foundToken = _oAuthTokensDictionary.TryGetValue(_feeBayUser, out string token);
            if(foundToken)
            {
                return token;
            }
            return string.Empty;
        }

        public void Reset(string feeBayUser) => _oAuthTokensDictionary.Remove(feeBayUser);
        #endregion

        #region Private Methods
        private string GetClientIdFromAppSettings()
        {
            string appId;
            appId = _configuration.GetValue<string>("FeeBayOAuthConnection:appid");
            return appId;
        }

        private string GetClientSecretFromAppSettings()
        {
            string certId;
            certId = _configuration.GetValue<string>("FeeBayOAuthConnection:certid");
            return certId;
        }

        private string GetRefreshTokenFromDataBase(string feeBayUser)
        {
            try
            {
                string refreshToken;
                refreshToken = _localDbConnectionManager.GetRefreshToken(feeBayUser);
                return refreshToken;
            } catch(Exception wellFuck)
            {
                throw;
            }
        }

        private DateTime GetUserTokenExpireTimeFromDataBase(string feeBayUser)
        {
            try
            {
                DateTime expireTime;
                expireTime = _localDbConnectionManager.GetUserTokenExpireTime(feeBayUser);
                return expireTime;
            } catch(Exception wellFuck)
            {
                throw;
            }
        }

        private string GetUserTokenFromDataBase(string feeBayUser)
        {
            try
            {
                string userToken;
                userToken = _localDbConnectionManager.GetUserToken(feeBayUser);
                return userToken;
            } catch(Exception wellFuck)
            {
                throw;
            }
        }

        private void HandleGetUserTokenError() => throw new NotImplementedException();

        private void SaveUserTokenToDatabase(Get_UserToken_Response oAuthUserTokenResponse, string feeBayUser)
        {
            DateTime newExpireTime = DateTime.Now.Add(new TimeSpan(0, 0, oAuthUserTokenResponse.expires_in));
            newExpireTime = newExpireTime.ToLocalTime();
            var success = _localDbConnectionManager.SaveUserToken(
                oAuthUserTokenResponse.access_token,
                newExpireTime,
                feeBayUser);
        }

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
        private Dictionary<string, string> _oAuthTokensDictionary = new Dictionary<string, string>();
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILocalDbConnectionManager _localDbConnectionManager;
    #endregion Fields
    }
}
