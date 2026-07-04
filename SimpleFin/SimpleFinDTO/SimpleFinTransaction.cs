using System;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;
namespace SimpleFin.SimpleFinDTO
{
   
    public class SimpleFinTransaction
    {
        [JsonPropertyName("id")]
        public string TransactionId { get; set; }
        [JsonPropertyName("posted")]
        public long PostedDateUnix { get; set; }
        public DateTimeOffset PostedDate { get { return DateTimeOffset.FromUnixTimeSeconds(PostedDateUnix); } }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
        [JsonPropertyName("payee")]
        public string Payee { get; set; }
        [JsonPropertyName("memo")]
        public string Memo { get; set; }
        [JsonPropertyName("transacted_at")]
        public long TransactedAtDateUnix { get; set; }
        [JsonPropertyName("mcc")]
        public string Mcc { get; set; }
    }
}
