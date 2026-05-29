using EbaySharp.Entities.Develop.KeyManagement.SigningKey;
using LocalDBConnections.StampDataDB.StampDataEntities;
using LocalDBConnections.StampDataDB.StampDataRepositories;
using MicroOrm.Dapper.Repositories.SqlGenerator;
using System;
using System.Data;
using System.Data.SqlClient;

namespace LocalDBConnections
{
    public class LocalDbConnectionManager : ILocalDbConnectionManager
    {
        #region Constants and Fields
        private const string _stampDataConnection = "Data Source=SERVER\\SQLEXPRESS;Initial Catalog=StampData;User ID=GenericUser;Password=Mishmash@2!;TrustServerCertificate=True";

        private readonly FeeBayOAuthTokensRepository _feeBayOAuthTokensRepository;
        private readonly FeeBaySigningKeyRepository _feeBaySigningKeysRepository;
        #endregion

        #region Constructors
        public LocalDbConnectionManager()
        {
            _feeBayOAuthTokensRepository = CreateFeeBayOAuthTokensRepository(_stampDataConnection);
            _feeBaySigningKeysRepository = CreateFeeBaySigningKeysRepository(_stampDataConnection);
        }

      
        #endregion

        #region Token Read Operations
        public string? GetRefreshToken(string feeBayUser)
        {
            var feeBayOAuthTokenData = _feeBayOAuthTokensRepository.Find(l => l.FeeBayUserName == feeBayUser);
            return feeBayOAuthTokenData?.RefreshToken;
        }

        public DateTime? GetRefreshTokenExpireTime(string feeBayUser)
        {
            var feeBayOAuthTokenData = _feeBayOAuthTokensRepository.Find(l => l.FeeBayUserName == feeBayUser);
            return feeBayOAuthTokenData?.RefreshTokenExpire;
        }

        public string? GetUserToken(string feeBayUser)
        {
            var feeBayOAuthTokenData = _feeBayOAuthTokensRepository.Find(l => l.FeeBayUserName == feeBayUser);
            return feeBayOAuthTokenData?.OAuthToken;
        }

        public DateTime? GetUserTokenExpireTime(string feeBayUser)
        {
            var feeBayOAuthTokenData = _feeBayOAuthTokensRepository.Find(l => l.FeeBayUserName == feeBayUser);
            return feeBayOAuthTokenData?.OAuthTokenExpire;
        }
        #endregion

        #region Token Write Operations
        public bool SaveUserToken(FeeBayOAuthTokens tokens)
        {
            var feeBayOAuthTokenData = _feeBayOAuthTokensRepository.Find(l => l.FeeBayUserName == tokens.FeeBayUserName);

            if (feeBayOAuthTokenData == null)
                return false;

            feeBayOAuthTokenData.OAuthToken = tokens.OAuthToken;
            feeBayOAuthTokenData.OAuthTokenExpire = tokens.OAuthTokenExpire;
            feeBayOAuthTokenData.RefreshToken = tokens.RefreshToken;
            feeBayOAuthTokenData.RefreshTokenExpire = tokens.RefreshTokenExpire;

            return _feeBayOAuthTokensRepository.Update(feeBayOAuthTokenData);
        }

        public bool SaveUserToken(string access_token, DateTime expires_in, string feeBayUser)
        {
            var feeBayOAuthTokenData = _feeBayOAuthTokensRepository.Find(l => l.FeeBayUserName == feeBayUser);
            if (feeBayOAuthTokenData == null)
                return false;
            feeBayOAuthTokenData.OAuthToken = access_token;
            feeBayOAuthTokenData.OAuthTokenExpire = expires_in;

            return _feeBayOAuthTokensRepository.Update(feeBayOAuthTokenData);
        }
        #endregion

        #region Repository Initialization
        private static FeeBayOAuthTokensRepository CreateFeeBayOAuthTokensRepository(string stampDataConnection)
        {
            IDbConnection dbConnection = new SqlConnection(stampDataConnection);
            return new FeeBayOAuthTokensRepository(dbConnection, new SqlGenerator<FeeBayOAuthTokens>());
        }
        private FeeBaySigningKeyRepository CreateFeeBaySigningKeysRepository(string stampDataConnection)
        {
            IDbConnection dbConnection = new SqlConnection(stampDataConnection);
            return new FeeBaySigningKeyRepository(dbConnection, new SqlGenerator<FeeBaySigningKey>());

        }
        public async Task<bool> SaveSigningKeyAsync(FeeBaySigningKey signingKey)
        {
            try
            {
                var asdf = await _feeBaySigningKeysRepository.FindAsync();
          
            if (asdf == null) 
            {
                return await _feeBaySigningKeysRepository.InsertAsync(signingKey);  
            }
               
            return await _feeBaySigningKeysRepository.UpdateAsync(signingKey);  
            }
            catch (Exception fuck)
            {

                throw;
            }
        }
        public async Task<FeeBaySigningKey> GetSigningKeyAsync()
        {
            try
            {
                var feeBaySigningKey = await _feeBaySigningKeysRepository.FindAllAsync();
                return feeBaySigningKey.FirstOrDefault();
              //  FeeBaySigningKey feeBaySigningKey = await _feeBaySigningKeysRepository.FindAsync();
                if (feeBaySigningKey == null) return null;

                return null;
            }
            catch (Exception shit)
            {

                throw;
            }
        }
        #endregion
    }
}
