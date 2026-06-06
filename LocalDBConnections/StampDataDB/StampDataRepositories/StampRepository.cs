using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Dapper;

namespace LocalDBConnections.StampDataDB.StampDataRepositories
{
    internal class StampRepository 
    {
        private readonly string _connectionString;

        public StampRepository(IDbConnection connection) 
        {
            // Extract connection string from the provided connection
            _connectionString = connection.ConnectionString ?? throw new ArgumentException("Connection string cannot be null or empty", nameof(connection));
        }

        public Decimal? GetStampCostById(int stampId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var result = connection.Query<Decimal?>("SELECT Cost FROM Stamps WHERE (StampId =@stampId)", new { stampId })
                    .SingleOrDefault();
                return result;
            }
        }

        public async Task<Decimal?> GetStampCostByIdAsync(int stampId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return (await connection.QueryAsync<Decimal?>("Select Cost From Stamps WHERE StampId =@stampId", new { stampId }))
                    .SingleOrDefault();
            }
        }

        public async Task<List<string>> GetSoldStamps(string orderNumber)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                //use this to get to orderitems to get skus in a dsd website order
                var result = await connection.QueryAsync<string>("SELECT SKU FROM ORDER_LINE_ITEMS  WHERE Order_Id = @orderNumber", new { orderNumber });
                return result.ToList();
            }
        }
    }
}

