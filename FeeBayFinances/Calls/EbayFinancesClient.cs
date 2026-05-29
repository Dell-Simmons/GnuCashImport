using FeeBayFinances;
using FeeBayFinances.Calls;

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
    }
}