

using LocalDBConnections.StampDataDB.StampDataEntities;
using System.Data;

namespace LocalDBConnections.StampDataDB.StampDataRepositories
{
    public class FeeBayOAuthTokensRepository : MicroOrm.Dapper.Repositories.DapperRepository<FeeBayOAuthTokens>
    {
        public FeeBayOAuthTokensRepository(IDbConnection connection, MicroOrm.Dapper.Repositories.SqlGenerator.ISqlGenerator<FeeBayOAuthTokens> sqlGenerator)
            : base(connection, sqlGenerator)
        {

        }
    }
}
