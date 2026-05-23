using LocalDBConnections.StampDataDB.StampDataEntities;
using LocalDBConnections.StampDataDB.StampDataRepositories;
using MicroOrm.Dapper.Repositories.SqlGenerator;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace LocalDBConnections
{
    public class LocalDbConnectionManager : ILocalDbConnectionManager
    {
        #region Constants and Fields
        private const string StampDataConnection = "Data Source=SERVER\\SQLEXPRESS;Initial Catalog=StampData;User ID=GenericUser;Password=Mishmash@2!;TrustServerCertificate=True";
        
      //  private readonly IConfiguration _configuration;
        //private readonly ILogger _logger;
       // private readonly string _pathToStockImages;
        private FeeBayOAuthTokensRepository _feeBayOAuthTokensRepository;
     //   private LogRepository _logRepository;
        #endregion

        #region Events
      //  public event System.EventHandler<MyEventArgs<string>> DataBaseTableChanged;
        #endregion
        
     //   public string PathToStockImages { get { return _pathToStockImages; } }
        
        #region Constructors
        public LocalDbConnectionManager()
        {
         //   _logger = logger;
          //  _configuration = configuration;
            SetUpStampDataRepositories(StampDataConnection);
        //    _ = DeleteOldNLogRecordsAsync(24);
        //    _pathToStockImages = _configuration.GetValue<string>("Scans:PathToStockImages");
        }
        #endregion

        #region Methods
        public string GetRefreshToken(string feeBayUser)
        {
            var feeBayOAuthTokenData = _feeBayOAuthTokensRepository.Find(l => l.FeeBayUserName == feeBayUser);
            return feeBayOAuthTokenData.RefreshToken;
        }
        
        public DateTime GetRefreshTokenExpireTime(string feeBayUser)
        {
            var feeBayOAuthTokenData = _feeBayOAuthTokensRepository.Find(l => l.FeeBayUserName == feeBayUser);
            return feeBayOAuthTokenData.RefreshTokenExpire;
        }
        
        public string GetUserToken(string feeBayUser)
        {
            var feeBayOAuthTokenData = _feeBayOAuthTokensRepository.Find(l => l.FeeBayUserName == feeBayUser);
            return feeBayOAuthTokenData.OAuthToken;
        }
        
        public DateTime GetUserTokenExpireTime(string feeBayUser)
        {
            var feeBayOAuthTokenData = _feeBayOAuthTokensRepository.Find(l => l.FeeBayUserName == feeBayUser);
            return feeBayOAuthTokenData.OAuthTokenExpire;
        }
        
        public bool SaveUserToken(FeeBayOAuthTokens tokens)
        {
            var feeBayOAuthTokenData = _feeBayOAuthTokensRepository.Find(l => l.FeeBayUserName == tokens.FeeBayUserName);

            feeBayOAuthTokenData.OAuthToken = tokens.OAuthToken;
            feeBayOAuthTokenData.OAuthTokenExpire = tokens.OAuthTokenExpire;
            feeBayOAuthTokenData.RefreshToken = tokens.RefreshToken;
            feeBayOAuthTokenData.RefreshTokenExpire = tokens.RefreshTokenExpire;
            var success = _feeBayOAuthTokensRepository.Update(feeBayOAuthTokenData);
            return success;
        }
        
        public bool SaveUserToken(string access_token, DateTime expires_in, string feeBayUser)
        {
            var feeBayOAuthTokenData = _feeBayOAuthTokensRepository.Find(l => l.FeeBayUserName == feeBayUser);
            feeBayOAuthTokenData.OAuthToken = access_token;
            feeBayOAuthTokenData.OAuthTokenExpire = expires_in;
            var success = _feeBayOAuthTokensRepository.Update(feeBayOAuthTokenData);
            return success;
        }
        
     
        private void SetUpStampDataRepositories(string stampDataConnection)
        {
            IDbConnection dbConnection = new SqlConnection(stampDataConnection);
            _feeBayOAuthTokensRepository = new FeeBayOAuthTokensRepository(dbConnection, new SqlGenerator<FeeBayOAuthTokens>());
         //   _logRepository = new LogRepository(stampDataConnection);
        }
        #endregion
    }
}