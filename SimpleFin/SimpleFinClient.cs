namespace SimpleFin
{
    using SimpleFin.SimpleFinDTO;
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
        AccountResponse jsonResponse = await FetchAccountDataAsync(accessUrl,string.Empty);
            List<SimpleFinTransaction> transactions = jsonResponse.Accounts.First().Transactions;
        
        Console.WriteLine("Account Response num of transactions:");
        Console.WriteLine(jsonResponse.Accounts.First().Transactions.Count);
        
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
    public static async Task<AccountResponse> FetchAccountDataAsync(string accessToken,string sourceId)
    {
        //! THIS WORKS FINE PEAK CU DOES NOT PROVIDE DATA MORE THAN 90 DAYS OLD THAT
        //! IS WHY YOU GET NOTHING WHEN ASKING FOR JANUARY OR FEBRUARY TRANSACTIONS!!
        // Parse the Access URL to extract credentials and the API path
        Uri uri = new Uri(accessToken);
        string userInfo = uri.UserInfo; // "username:password"
      //  string businessCheckingId = "ACT-889f0bab-1c17-4dba-b790-21c3facbc0e6";
       // string businessSavingsId = "ACT-02271871-782c-42b9-8cda-61431677ac40";
       // string businessVisaId = "ACT-695f807d-f9f6-48b3-986a-b0ad3f177d74";
        //    string sparkBusinessCC = "ACT-fe0b66ca-58d4-40fe-8620-7352f97ebcca";
           long startDate = new DateTimeOffset(
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .ToUnixTimeSeconds();

            long endDate = new DateTimeOffset(
                new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc))
                .ToUnixTimeSeconds();
            string apiUrl = 
                $"{uri.Scheme}://{uri.Host}:{uri.Port}{uri.AbsolutePath}/accounts?version=2&" +
                $"account={sourceId}&" +
                $"start-date={startDate}&" +
                $"end-date={endDate}";
            // string apiUrl = 
            //     $"{uri.Scheme}://{uri.Host}:{uri.Port}{uri.AbsolutePath}/accounts?version=2&" +
            //     $"account={businessCheckingId}&" +
            //     $"account={sparkBusinessCC}&" +
            //     $"start-date={startDate}&" +
            //     $"end-date={endDate}";
         
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        
        // Attach HTTP Basic Authentication
        var authBytes = Encoding.ASCII.GetBytes(userInfo);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        using HttpClient client = new HttpClient();
        var response = await client.SendAsync(request);
            //response.EnsureSuccessStatusCode();
          
            var look = await response.Content.ReadAsStringAsync();
            // Some APIs return numeric values as JSON strings (e.g. "balance": "123.45").
            // Allow reading numbers from strings so deserialization won't fail when that
            // happens.
            AccountResponse? data = JsonSerializer.Deserialize<AccountResponse>(
             look,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                });

        return data;
    }
    
    }
}
