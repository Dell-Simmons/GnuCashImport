using MicroOrm.Dapper.Repositories.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalDBConnections.StampDataDB.StampdataEntities
{
    [System.ComponentModel.DataAnnotations.Schema.Table("Order_Line_Items_By_Order_Id")]
    
    
        public class Order_Line_Items_By_Order_Id
    {
        [System.ComponentModel.DataAnnotations.Key]
        public string SKU { get; set; }
        public string Order_Id { get; set; }
        public decimal Sales_Price { get; set; }
        public decimal Cost { get; set; }
    }
}
