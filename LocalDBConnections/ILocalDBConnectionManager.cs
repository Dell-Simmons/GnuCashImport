//using LocalDBConnections.CatValuesDB.DTOs;
//using LocalDBConnections.StampDataDB.DTOs;
using LocalDBConnections.StampDataDB.StampDataEntities;
using LocalDBConnections.StampDataDB.StampdataEntities;
//using SIDSUtilities48;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LocalDBConnections
{
    public interface ILocalDbConnectionManager
    {
        Task<List<Order_Line_Items_By_Order_Id>> GetSoldNopStampsViewAsync(string nopOrderId);

        #region Events
        // event EventHandler<MyEventArgs<string>> DataBaseTableChanged;
        #endregion

        #region Methods 
        decimal GetStampCOGS(string sku);
        string? GetRefreshToken(string feeBayUser);
        DateTime? GetRefreshTokenExpireTime(string feeBayUser);
        string? GetUserToken(string feeBayUser);
        DateTime? GetUserTokenExpireTime(string feeBayUser);
        bool SaveUserToken(FeeBayOAuthTokens tokens);
        bool SaveUserToken(string access_token, DateTime expireTime, string feeBayUser);
        Task<bool> SaveSigningKeyAsync(FeeBaySigningKeys signingKey);
        Task<FeeBaySigningKeys?> GetSigningKeyAsync();
        Task<bool> SaveSimpleFinAccessToken(SimpleFinAccessTokens bankAccessToken);
        Task<SimpleFinAccessTokens> GetSimpleFinAccessToken(string bankName);
        Task<List<string>> GetSoldNopStampsAsync(string nopOrderId);
        //  decimal GetStampCostById(string sku);
        #endregion
        // IEnumerable<MisslItem> GetMisslItems();
    }
}