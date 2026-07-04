namespace SimpleFin.SimpleFinDTO
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    public class AccountResponse
    {
        [JsonPropertyName("accounts")]
        public List<Account> Accounts { get; set; } = new();
    }
}
