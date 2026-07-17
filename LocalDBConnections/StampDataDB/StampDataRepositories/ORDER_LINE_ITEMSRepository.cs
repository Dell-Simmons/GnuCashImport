using LocalDBConnections.StampDataDB.StampdataEntities;
using System.Data;

namespace LocalDBConnections.StampDataDB.StampDataRepositories
{
    public class ORDER_LINE_ITEMSRepository : MicroOrm.Dapper.Repositories.DapperRepository<ORDER_LINE_ITEM>
    {
        public ORDER_LINE_ITEMSRepository(IDbConnection connection, MicroOrm.Dapper.Repositories.SqlGenerator.ISqlGenerator<ORDER_LINE_ITEM> sqlGenerator)
            : base(connection, sqlGenerator)
        {

        }
    }
}
