# FeeBayOAuth.TokenFactory

A reusable class library for managing the complete eBay OAuth 2.0 lifecycle, including initial authorization, token retrieval, refresh, and caching.

## Purpose

This library was extracted from the `FeeBayOAuthConnection` project to make OAuth token management functionality easily reusable across multiple solutions. It provides a complete OAuth 2.0 implementation for eBay, handling:

- **Initial Authorization**: Exchange authorization codes for refresh tokens (authorization code grant flow)
- **Token Refresh**: Automatically refresh access tokens using stored refresh tokens
- **Token Caching**: In-memory caching to avoid unnecessary API calls
- **Token Persistence**: Database storage for long-term token management

## Key Components

### RefreshTokenManager

Manages the initial OAuth authorization flow to obtain refresh tokens from eBay. This is typically used once during setup when a user first authorizes your application.

**Key Method:**
- `GetNewRefreshTokenFromFeeBay(string userAuthCode, string feeBayUserId)` - Exchanges an authorization code for refresh and access tokens

**Usage Scenario:** 
1. User clicks "Authorize with eBay" in your app
2. eBay redirects user to consent page
3. User approves, eBay redirects back with authorization code
4. You call this method to exchange the code for tokens
5. Tokens are saved to database for future use

### OAuthTokenFactory

The main class that manages OAuth access tokens for ongoing API operations. It handles token retrieval, expiration checking, automatic refresh, and caching.

**Key Methods:**
- `GetOAuthToken(string feeBayUserName)` - Gets a valid OAuth access token (refreshes if expired)
- `Reset(string feeBayUser)` - Clears cached token (forces refresh on next call)

**Features:**
- Automatically checks token expiration
- Refreshes tokens within 10 minutes of expiration
- Maintains in-memory cache for performance
- Supports multiple eBay users simultaneously

### Get_User_Token

Static helper class that makes HTTP calls to eBay's OAuth 2.0 token endpoint to refresh access tokens.

**Key Method:**
- `MakeCall(string refreshToken, IHttpClientFactory httpClientFactory, string appId, string certId, out Response_Errors errorsContainer)` - Exchanges a refresh token for a new access token

### OAuthHttpHelpers

Shared utility class providing common HTTP helper methods used across OAuth operations.

**Methods:**
- `CreateAuthorizationHeader(string appId, string certId)` - Creates Basic Auth header
- `CreateRequestPayload(Dictionary<string, string> payloadParams)` - Creates URL-encoded payload

## Dependencies

### NuGet Packages
- `Microsoft.Extensions.Http` - For IHttpClientFactory
- `Microsoft.Extensions.Configuration.Binder` - For IConfiguration
- `Newtonsoft.Json` - For JSON serialization/deserialization

### Project References
- `LocalDBConnections` - For database access (ILocalDbConnectionManager)
- `SIDSUtilities48` - For AppSettingsHelpers utility

## Configuration

The library expects the following configuration keys in `appsettings.json`:

```json
{
  "FeeBayOAuthConnection": {
	"appid": "your-ebay-app-id",
	"certid": "your-ebay-cert-id",
	"redirectUri": "your-redirect-uri",
	"username": {
	  "OAuthToken": "cached-access-token",
	  "OAuthTokenExpire": "2024-01-01T12:00:00",
	  "RefreshToken": "refresh-token"
	}
  }
}
```

## Complete OAuth Flow

### 1. Initial Setup (One-Time Authorization)

```csharp
// Register in dependency injection
services.AddSingleton<RefreshTokenManager>();
services.AddSingleton<OAuthTokenFactory>();

// Controller handling eBay OAuth callback
public class OAuthController : Controller
{
	private readonly RefreshTokenManager _refreshTokenManager;

	public OAuthController(RefreshTokenManager refreshTokenManager)
	{
		_refreshTokenManager = refreshTokenManager;
	}

	// eBay redirects here after user authorization
	public IActionResult Callback(string code, string state)
	{
		// Exchange authorization code for tokens
		bool success = _refreshTokenManager.GetNewRefreshTokenFromFeeBay(code, "myEbayUser");

		if (success)
		{
			return RedirectToAction("Success");
		}
		return RedirectToAction("Error");
	}
}
```

### 2. Ongoing API Calls

```csharp
public class EbayService
{
	private readonly OAuthTokenFactory _tokenFactory;
	private readonly IHttpClientFactory _httpClientFactory;

	public EbayService(OAuthTokenFactory tokenFactory, IHttpClientFactory httpClientFactory)
	{
		_tokenFactory = tokenFactory;
		_httpClientFactory = httpClientFactory;
	}

	public async Task<string> GetUserInventory()
	{
		// Get valid access token (automatically refreshed if needed)
		string accessToken = _tokenFactory.GetOAuthToken("myEbayUser");

		// Use token for eBay API calls
		var client = _httpClientFactory.CreateClient();
		client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

		var response = await client.GetAsync("https://api.ebay.com/sell/inventory/v1/inventory_item");
		return await response.Content.ReadAsStringAsync();
	}

	public void ForceTokenRefresh()
	{
		// Clear cache to force fresh token retrieval
		_tokenFactory.Reset("myEbayUser");
	}
}
```

## eBay OAuth 2.0 Scopes

The library requests the following scopes by default:
- `https://api.ebay.com/oauth/api_scope` - General API access
- `https://api.ebay.com/oauth/api_scope/sell.marketing` - Marketing campaigns
- `https://api.ebay.com/oauth/api_scope/sell.inventory` - Inventory management
- `https://api.ebay.com/oauth/api_scope/sell.account` - Account settings
- `https://api.ebay.com/oauth/api_scope/sell.fulfillment` - Order fulfillment
- `https://api.ebay.com/oauth/api_scope/sell.analytics.readonly` - Analytics (read-only)
- `https://api.ebay.com/oauth/api_scope/sell.stores` - eBay store management

## Target Framework

- .NET 10.0

## Architecture Notes

- **Separation of Concerns**: Initial authorization (`RefreshTokenManager`) is separate from token refresh (`OAuthTokenFactory`)
- **Automatic Refresh**: Tokens are refreshed automatically when they expire or within 10 minutes of expiration
- **Thread-Safe Caching**: In-memory dictionary maintains cached tokens per user
- **Database-First**: Primary token storage is database; configuration is fallback
- **Reusable Utilities**: Shared HTTP helpers eliminate code duplication

## Token Lifecycle

1. **Authorization** → User grants permission, app gets authorization code
2. **Initial Exchange** → `RefreshTokenManager` exchanges code for refresh + access tokens
3. **Storage** → Tokens saved to database with expiration times
4. **Ongoing Use** → `OAuthTokenFactory` retrieves tokens and checks expiration
5. **Auto-Refresh** → If expired/expiring, `Get_User_Token` refreshes access token
6. **Caching** → Valid tokens cached in memory to minimize database calls

## Error Handling

The library returns `null` or `false` on errors. Check for:
- Invalid authorization codes (initial setup)
- Expired refresh tokens (user must re-authorize)
- Network failures (HTTP exceptions)
- Invalid configuration (missing app ID, cert ID, etc.)

## Notes

- Access tokens expire after ~2 hours (eBay's default)
- Refresh tokens expire after 18 months (eBay's default)
- Call `Reset(userName)` to clear cache and force token refresh
- The factory maintains one cached token per eBay user
- All API calls use HTTPS to eBay's production endpoints
