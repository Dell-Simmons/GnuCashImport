using LocalDBConnections.StampDataDB.StampdataEntities;
using System.Data;

namespace LocalDBConnections.StampDataDB.StampDataRepositories
{
    public class Order_Line_Items_By_Order_IdRepository : MicroOrm.Dapper.Repositories.DapperRepository<Order_Line_Items_By_Order_Id>
    {
        public Order_Line_Items_By_Order_IdRepository(IDbConnection connection, MicroOrm.Dapper.Repositories.SqlGenerator.ISqlGenerator<Order_Line_Items_By_Order_Id> sqlGenerator)
            : base(connection, sqlGenerator)
        {

        }
    }
}
