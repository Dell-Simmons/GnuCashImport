using System;
using System.Text.Json.Serialization;
namespace SimpleFin.SimpleFinDTO
{
   
    public class SimpleFinTransaction
    {
        [JsonPropertyName("posted")]
        public long PostedDateUnix { get; set; }
        public DateTimeOffset PostedDate { get { return DateTimeOffset.FromUnixTimeSeconds(PostedDateUnix); } }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }
}
