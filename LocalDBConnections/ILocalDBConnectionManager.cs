//using LocalDBConnections.CatValuesDB.DTOs;
//using LocalDBConnections.StampDataDB.DTOs;
using LocalDBConnections.StampDataDB.StampDataEntities;
//using SIDSUtilities48;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LocalDBConnections
{
    public interface ILocalDbConnectionManager
    {
        decimal? GetStampCOGS(int sku);
        #region Events
        // event EventHandler<MyEventArgs<string>> DataBaseTableChanged;
        #endregion

        #region Methods
        string? GetRefreshToken(string feeBayUser);
        DateTime? GetRefreshTokenExpireTime(string feeBayUser);
        string? GetUserToken(string feeBayUser);
        DateTime? GetUserTokenExpireTime(string feeBayUser);
        bool SaveUserToken(FeeBayOAuthTokens tokens);
        bool SaveUserToken(string access_token, DateTime expireTime, string feeBayUser);
        Task<bool> SaveSigningKeyAsync(FeeBaySigningKeys signingKey);
        Task<FeeBaySigningKeys?> GetSigningKeyAsync();
        #endregion
        // IEnumerable<MisslItem> GetMisslItems();
    }
}