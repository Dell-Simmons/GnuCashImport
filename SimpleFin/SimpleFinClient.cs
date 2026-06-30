namespace SimpleFin
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class SimpleFinClient()
    {
        public static async Task<string> Connect3(string setupToken)
        {
            string _setupToken = setupToken;
            string accessUrl = await ClaimAccessUrlAsync(_setupToken);
            Console.WriteLine($"Claimed Access URL: {accessUrl}");
            return accessUrl;
        }
        public static async Task Connect2(string setupToken)
        {
            string _setupToken = setupToken;
               // 1. Claim your unique Access URL using your setup token
        string accessUrl = await ClaimAccessUrlAsync(_setupToken);

        Console.WriteLine($"Claimed Access URL: {accessUrl}");

        // 2. Fetch data from the claimed Access URL
        string jsonResponse = await FetchAccountDataAsync(accessUrl);
        
        Console.WriteLine("Account Data:");
        Console.WriteLine(jsonResponse);
        
        // Note: You can parse this JSON into custom C# models for your app
   
        }
          private static async Task<string> ClaimAccessUrlAsync(string setupToken)
    {
        //!  Once you receive an ACCESS_URL, save it—the corresponding SETUP_TOKEN will no longer work.
        // Decode the base64 claim URL provided by SimpleFIN
        byte[] bytes = Convert.FromBase64String(setupToken);
        string claimUrl = Encoding.UTF8.GetString(bytes);
        using HttpClient client = new HttpClient();
        // Make the POST request to claim your permanent Access URL
        var response = await client.PostAsync(claimUrl, null);
       // response.EnsureSuccessStatusCode();
        var look = await response.Content.ReadAsStringAsync();
        return look;
    }
  private static async Task<string> FetchAccountDataAsync(string accessUrl)
    {
        // Parse the Access URL to extract credentials and the API path
        Uri uri = new Uri(accessUrl);
        string userInfo = uri.UserInfo; // "username:password"
        
        string apiUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}{uri.AbsolutePath}/accounts?version=2";

        var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        
        // Attach HTTP Basic Authentication
        var authBytes = Encoding.ASCII.GetBytes(userInfo);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        using HttpClient client = new HttpClient();
        var response = await client.SendAsync(request);
        //response.EnsureSuccessStatusCode();
        var look = await response.Content.ReadAsStringAsync();
        return look;
    }
        public  async Task Connect()
        {
            // 1. Get a Setup Token
           // Console.Write("Setup Token? ");
           // string setupToken = Console.ReadLine()!;
            string setupToken = "aHR0cHM6Ly9iZXRhLWJyaWRnZS5zaW1wbGVmaW4ub3JnL3NpbXBsZWZpbi9jbGFpbS8wODQ2Q0Q5RUYxNURGQjVBM0IxRThFQkEzRDM4NDVCMTBCRTMwODhDQjk1RjM4NTY4MEZCOUJBOTc2QUI3MkY5QTUwNUM3NjFEREQ1RDk5N0JDRTZCMzU4MzM3ODZDMkI2NUVFMDMxMUY0MzVFNDEyNjE1NzkzNTdCMDkwRDNGQw=="; // Replace with your actual setup token
            using HttpClient client = new HttpClient();

            // 2. Claim an Access URL
            //!  Once you receive an ACCESS_URL, save it—the corresponding SETUP_TOKEN will no longer work.
            string claimUrl = Encoding.UTF8.GetString(Convert.FromBase64String(setupToken));

            HttpResponseMessage response = await client.PostAsync(claimUrl, null);
           // response.EnsureSuccessStatusCode();

            string accessUrl = await response.Content.ReadAsStringAsync();

            // Parse the access URL
            Uri uri = new Uri(accessUrl);

            string username = Uri.UnescapeDataString(uri.UserInfo.Split(':')[0]);
            string password = Uri.UnescapeDataString(uri.UserInfo.Split(':')[1]);

            string url = $"{uri.Scheme}://{uri.Host}:{uri.Port}/accounts?version=2";

            // Basic Authentication
            string auth =
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", auth);

            response = await client.GetAsync(url);
           // response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            AccountResponse? data = JsonSerializer.Deserialize<AccountResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (data == null)
                return;

            // Process the accounts
            foreach (Account account in data.Accounts)
            {
                DateTime balanceDate =
                    DateTimeOffset.FromUnixTimeSeconds(account.BalanceDate).LocalDateTime;

                Console.WriteLine();
                Console.WriteLine($"{balanceDate} {account.Balance,8} {account.Name}");
                Console.WriteLine(new string('-', 60));

                foreach (Transaction transaction in account.Transactions)
                {
                    DateTime posted =
                        DateTimeOffset.FromUnixTimeSeconds(transaction.Posted).LocalDateTime;

                    Console.WriteLine(
                        $"{posted} {transaction.Amount,8} {transaction.Description}");
                }
            }
        }
    }

    public class AccountResponse
    {
        [JsonPropertyName("accounts")]
        public List<Account> Accounts { get; set; } = new();
    }

    public class Account
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("balance-date")]
        public long BalanceDate { get; set; }

        [JsonPropertyName("transactions")]
        public List<Transaction> Transactions { get; set; } = new();
    }

    public class Transaction
    {
        [JsonPropertyName("posted")]
        public long Posted { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }
}
