using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace FeeBayFinances.Calls
{
    public class EbayFinancesClient
    {
        private readonly HttpClient _httpClient;

        public EbayFinancesClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Calls eBay Finances API getSellerFundsSummary
        /// </summary>
        /// <param name="userAccessToken">Valid OAuth user token</param>
        /// <returns>SellerFundsSummaryResponse</returns>
        public async Task<SellerFundsSummaryResponse> GetSellerFundsSummaryAsync(string userAccessToken)
        {
            if (string.IsNullOrWhiteSpace(userAccessToken))
                throw new ArgumentException("Access token is required.");

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://apiz.ebay.com/sell/finances/v1/seller_funds_summary"
            );

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", userAccessToken);

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request);

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"eBay API Error {(int)response.StatusCode}: {json}");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<SellerFundsSummaryResponse>(json, options);
        }
    }
}