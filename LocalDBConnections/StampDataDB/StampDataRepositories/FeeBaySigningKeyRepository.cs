using LocalDBConnections.StampDataDB.StampDataEntities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace LocalDBConnections.StampDataDB.StampDataRepositories
{
    public class FeeBaySigningKeyRepository : MicroOrm.Dapper.Repositories.DapperRepository<FeeBaySigningKeys>
    {
        public FeeBaySigningKeyRepository(IDbConnection connection, MicroOrm.Dapper.Repositories.SqlGenerator.ISqlGenerator<FeeBaySigningKeys> sqlGenerator)
            : base(connection, sqlGenerator)
        {

        }
    }
}
