using System;
using System.Collections.Generic;
using System.Text;
using Dapper.Contrib.Extensions;

namespace LocalDBConnections.StampDataDB.StampDataEntities
{
    public class SimpleFinAccessTokens
    {
        [Key]
        public string? BankName { get; set; }
        public string? AccessToken { get; set; }
    }
}
