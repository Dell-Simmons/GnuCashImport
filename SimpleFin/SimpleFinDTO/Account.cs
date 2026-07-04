namespace SimpleFin.SimpleFinDTO
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    public class Account
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("balance-date")]
        public long BalanceDateUnix { get; set; }
        public DateTimeOffset BalanceDate { get { return DateTimeOffset.FromUnixTimeSeconds(BalanceDateUnix); } }

        [JsonPropertyName("transactions")]
        public List<SimpleFinTransaction> Transactions { get; set; } = new();
    }
}
