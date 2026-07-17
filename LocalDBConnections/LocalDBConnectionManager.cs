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
        private readonly SimpleFinAccessTokenRepository _simpleFinAccessTokenRepository;
        private ORDER_LINE_ITEMSRepository _orderLineItemsRepository;
        private readonly StampRepository _stampRepository;
        #endregion

        #region Constructors
        public LocalDbConnectionManager()
        {
            _feeBayOAuthTokensRepository = CreateFeeBayOAuthTokensRepository(_stampDataConnection);
            _feeBaySigningKeysRepository = CreateFeeBaySigningKeysRepository(_stampDataConnection);
            _simpleFinAccessTokenRepository = CreateSimpleFinAccessTokenRepository(_stampDataConnection);
            _stampRepository = CreateFeeBayStampRepository(_stampDataConnection);
        }

        private StampRepository CreateFeeBayStampRepository(string stampDataConnection)
        {
            IDbConnection dbConnection = new SqlConnection(stampDataConnection);
            return new StampRepository(dbConnection);//, new SqlGenerator<FeeBaySigningKeys>());
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
            public decimal GetStampCOGS(string sku)
        {
            var cogsFromStampDb = _stampRepository.GetStampCostById(sku) ?? 0m;
            return cogsFromStampDb;

        }
        #region Token Write Operations
        public bool SaveUserToken(FeeBayOAuthTokens tokens)
        {
            var feeBayOAuthTokenData = _feeBayOAuthTokensRepository.Find(l => l.FeeBayUserName == tokens.FeeBayUserName);

            if(feeBayOAuthTokenData == null)
            {
                return false;
            }

            feeBayOAuthTokenData.OAuthToken = tokens.OAuthToken;
            feeBayOAuthTokenData.OAuthTokenExpire = tokens.OAuthTokenExpire;
            feeBayOAuthTokenData.RefreshToken = tokens.RefreshToken;
            feeBayOAuthTokenData.RefreshTokenExpire = tokens.RefreshTokenExpire;

            return _feeBayOAuthTokensRepository.Update(feeBayOAuthTokenData);
        }

        public bool SaveUserToken(string access_token, DateTime expires_in, string feeBayUser)
        {
            var feeBayOAuthTokenData = _feeBayOAuthTokensRepository.Find(l => l.FeeBayUserName == feeBayUser);
            if(feeBayOAuthTokenData == null)
            {
                return false;
            }

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
            return new FeeBaySigningKeyRepository(dbConnection, new SqlGenerator<FeeBaySigningKeys>());
        }
        private SimpleFinAccessTokenRepository CreateSimpleFinAccessTokenRepository(string stampDataConnection)
        {
            IDbConnection dbConnection = new SqlConnection(stampDataConnection);
            return new SimpleFinAccessTokenRepository(dbConnection, new SqlGenerator<SimpleFinAccessTokens>());
        }
        public async Task<bool> SaveSigningKeyAsync(FeeBaySigningKeys signingKey) =>
                await _feeBaySigningKeysRepository.InsertAsync(signingKey);

        public async Task<FeeBaySigningKeys?> GetSigningKeyAsync()
        {
            var feeBaySigningKey = await _feeBaySigningKeysRepository.FindAllAsync();
            return feeBaySigningKey.FirstOrDefault();
            //  FeeBaySigningKey feeBaySigningKey = await _feeBaySigningKeysRepository.FindAsync();
            // if (feeBaySigningKey == null) return null;
        }

        public async Task<bool> SaveSimpleFinAccessToken(SimpleFinAccessTokens bankAccessToken)
        {
            return await _simpleFinAccessTokenRepository.InsertAsync(bankAccessToken);
        }
        public async Task<SimpleFinAccessTokens?> GetSimpleFinAccessToken(string bankName)
        {
            var allBankAccessTokens = await _simpleFinAccessTokenRepository.FindAllAsync();
            return allBankAccessTokens.Where(t => t.BankName == bankName).FirstOrDefault();
        }

        public async Task<List<string>> GetSoldNopStampsAsync(string nopOrderId)
        {
            var orderItems = _orderLineItemsRepository.FindAllAsync(x => x.Order_Id == nopOrderId).Result;
            var soldStamps = orderItems.Select(x => x.SKU).ToList();
            return soldStamps;
        }
        #endregion
    }
}

