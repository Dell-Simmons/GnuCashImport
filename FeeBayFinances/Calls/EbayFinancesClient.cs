using FeeBayFinances;
using FeeBayFinances.Calls;
using FeeBayFinances.Models;

namespace FeeBayFinances.Calls
{
    public class EbayFinancesClient : EbayApiClientBase
    {
        protected override string BaseUrl =>
            "https://apiz.ebay.com/sell/finances/v1";

        public EbayFinancesClient(
            HttpClient httpClient,
            IOAuthTokenService tokenService)
            : base(httpClient, tokenService)
        {
        }

        public Task<SellerFundsSummaryResponse>
            GetSellerFundsSummaryAsync()
        {
            return GetAsync<SellerFundsSummaryResponse>(
                "seller_funds_summary");
        }

        public Task<GetTransactionsResponse>
            GetTransactionsAsync(
                string filter = null,
                int? limit = null,
                int? offset = null,
                string sort = null)
        {
            var query = new List<string>();

            if (!string.IsNullOrWhiteSpace(filter))
                query.Add($"filter={Uri.EscapeDataString(filter)}");

            if (limit.HasValue)
                query.Add($"limit={limit.Value}");

            if (offset.HasValue)
                query.Add($"offset={offset.Value}");

            if (!string.IsNullOrWhiteSpace(sort))
                query.Add($"sort={Uri.EscapeDataString(sort)}");

            var url = "transaction";

            if (query.Count > 0)
                url += "?" + string.Join("&", query);

            return GetAsync<GetTransactionsResponse>(url);
        }
    }
}