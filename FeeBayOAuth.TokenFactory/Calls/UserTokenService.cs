using FeeBayOAuth.TokenService.Models;
using FeeBayOAuth.TokenService.Utilities;
using Newtonsoft.Json;
using System.Text;

namespace FeeBayOAuth.TokenService.Calls;

public class UserTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _appId;
    private readonly string _certId;

    public UserTokenService(IHttpClientFactory httpClientFactory, string appId, string certId)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _appId = appId ?? throw new ArgumentNullException(nameof(appId));
        _certId = certId ?? throw new ArgumentNullException(nameof(certId));
    }

    public async Task<UserTokenResult> GetUserTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token cannot be null or empty.", nameof(refreshToken));

        var payloadParams = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken }
        };

        var requestPayload = OAuthHttpHelpers.CreateRequestPayload(payloadParams);
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri("https://api.ebay.com/identity/v1/oauth2/");

        using var request = new HttpRequestMessage(HttpMethod.Post, "token")
        {
            Content = new StringContent(requestPayload, Encoding.UTF8, "application/x-www-form-urlencoded")
        };

        request.Headers.Add("Authorization", OAuthHttpHelpers.CreateAuthorizationHeader(_appId, _certId));

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var userToken = JsonConvert.DeserializeObject<Get_UserToken_Response>(responseContent);
                return UserTokenResult.Success(userToken);
            }
            else
            {
                var errors = JsonConvert.DeserializeObject<Response_Errors>(responseContent);
                return UserTokenResult.Failure(errors);
            }
        }
        catch (JsonException ex)
        {
            var error = new Response_Errors();
            return UserTokenResult.Failure(error);
        }
        catch (HttpRequestException ex)
        {
            var error = new Response_Errors();
            return UserTokenResult.Failure(error);
        }
    }

    private static IList<string> GetUserScopes()
    {
        return new List<string>
        {
            "https://api.ebay.com/oauth/api_scope",
            "https://api.ebay.com/oauth/api_scope/sell.marketing",
            "https://api.ebay.com/oauth/api_scope/sell.inventory",
            "https://api.ebay.com/oauth/api_scope/sell.account",
            "https://api.ebay.com/oauth/api_scope/sell.fulfillment",
            "https://api.ebay.com/oauth/api_scope/sell.analytics.readonly",
            "https://api.ebay.com/oauth/api_scope/sell.stores"
        };
    }

    private static string FormatScopesForRequest(IList<string> scopes)
    {
        if (scopes == null || scopes.Count == 0)
            return string.Empty;

        return Uri.EscapeDataString(string.Join(" ", scopes));
    }
}
