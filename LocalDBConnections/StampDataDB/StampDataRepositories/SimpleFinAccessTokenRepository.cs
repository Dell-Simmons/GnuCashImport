using LocalDBConnections.StampDataDB.StampDataEntities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace LocalDBConnections.StampDataDB.StampDataRepositories
{
    public class SimpleFinAccessTokenRepository : MicroOrm.Dapper.Repositories.DapperRepository<SimpleFinAccessTokens>
    {
        public SimpleFinAccessTokenRepository(IDbConnection connection, MicroOrm.Dapper.Repositories.SqlGenerator.ISqlGenerator<SimpleFinAccessTokens> sqlGenerator)
          : base(connection, sqlGenerator)
        {

        }
    }
}
