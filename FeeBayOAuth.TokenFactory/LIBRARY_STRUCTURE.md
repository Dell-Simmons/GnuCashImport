# FeeBayOAuth.TokenFactory - Library Structure

## Overview
Complete OAuth 2.0 lifecycle management for eBay integration.

## File Structure

```
FeeBayOAuth.TokenFactory/
│
├── FeeBayOAuth.TokenFactory.csproj          # Project file
├── README.md                                 # Complete documentation
│
├── OAuthTokenFactory.cs                      # Main token management class
│   └── Methods:
│       ├── GetOAuthToken(userName)          # Get/refresh access tokens
│       └── Reset(userName)                  # Clear cached tokens
│
├── OAuth2/
│   └── RefreshTokenManager.cs               # Initial authorization flow
│       └── GetNewRefreshTokenFromFeeBay()   # Exchange auth code for tokens
│
├── Calls/
│   └── Get_User_Token.cs                    # Token refresh API call
│       └── MakeCall()                       # HTTP request to eBay OAuth endpoint
│
├── DTO/
│   └── Response/
│       ├── Get_UserToken_Response.cs        # Access token response model
│       └── Response_Errors.cs               # Error response model
│
├── Models/
│   └── OAuthApiResponse.cs                  # Initial auth response model
│
└── Utilities/
	└── OAuthHttpHelpers.cs                  # Shared HTTP utilities
		├── CreateAuthorizationHeader()      # Base64 Basic Auth
		└── CreateRequestPayload()           # URL-encode parameters
```

## Data Flow

### Initial Setup (One-Time)
```
User Authorization
	↓
eBay Consent Page
	↓
Authorization Code
	↓
RefreshTokenManager.GetNewRefreshTokenFromFeeBay()
	↓
OAuthHttpHelpers (HTTP utilities)
	↓
eBay OAuth API
	↓
OAuthApiResponse (tokens)
	↓
Database (via ILocalDbConnectionManager)
```

### Ongoing Operations
```
OAuthTokenFactory.GetOAuthToken(user)
	↓
Check Memory Cache
	↓ (miss or expired)
Database Lookup
	↓
Check Expiration
	↓ (expired/expiring)
Get_User_Token.MakeCall()
	↓
OAuthHttpHelpers (HTTP utilities)
	↓
eBay OAuth API
	↓
Get_UserToken_Response (new access token)
	↓
Update Database & Cache
	↓
Return Valid Token
```

## Key Design Decisions

### 1. Code Deduplication
**Before:** Both `Get_User_Token` and `NewRefreshTokenManager` had duplicate helper methods.

**After:** Extracted to `OAuthHttpHelpers` utility class:
- `CreateAuthorizationHeader()` - Used by both authorization flows
- `CreateRequestPayload()` - Used by both HTTP requests

### 2. Separation of Concerns
- **RefreshTokenManager**: Initial setup (authorization code → refresh token)
- **OAuthTokenFactory**: Ongoing operations (refresh token → access token)
- **Get_User_Token**: Low-level HTTP call wrapper

### 3. Public API
All main classes are now `public` for external use:
- `RefreshTokenManager` (was `NewRefreshTokenManager`, internal)
- `OAuthTokenFactory` (was internal)
- `OAuthHttpHelpers` (new utility class)

### 4. Naming Improvements
- `NewRefreshTokenManager` → `RefreshTokenManager` (clearer purpose)
- Organized into logical namespaces:
  - `FeeBayOAuth.TokenFactory` - Main classes
  - `FeeBayOAuth.TokenFactory.OAuth2` - OAuth flow managers
  - `FeeBayOAuth.TokenFactory.Calls` - API wrappers
  - `FeeBayOAuth.TokenFactory.DTO.Response` - Response models
  - `FeeBayOAuth.TokenFactory.Models` - Request/response models
  - `FeeBayOAuth.TokenFactory.Utilities` - Shared helpers

## Dependencies

### External NuGet Packages
- `Microsoft.Extensions.Http` - HTTP client factory
- `Microsoft.Extensions.Configuration.Binder` - Configuration binding
- `Newtonsoft.Json` - JSON serialization

### Internal Project References
- `LocalDBConnections` - Database access for token persistence
- `SIDSUtilities48` - App settings helpers

## Integration Points

### From FeeBayOAuthConnection
```csharp
public class FeeBayOAuthConnectionManager
{
	private OAuthTokenFactory _oAuthTokenFactory;
	private RefreshTokenManager _refreshTokenManager;

	public FeeBayOAuthConnectionManager(...)
	{
		_oAuthTokenFactory = new OAuthTokenFactory(...);
		_refreshTokenManager = new RefreshTokenManager(...);
	}

	// Initial setup
	public bool GetNewRefreshTokenFromFeeBay(string userName, string code)
		=> _refreshTokenManager.GetNewRefreshTokenFromFeeBay(code, userName);

	// Ongoing operations
	private string GetToken(string userName)
		=> _oAuthTokenFactory.GetOAuthToken(userName);
}
```

## Testing Strategy

### Unit Test Coverage Areas
1. **OAuthHttpHelpers**
   - Base64 encoding correctness
   - URL encoding edge cases
   - Empty/null input handling

2. **RefreshTokenManager**
   - Authorization code exchange
   - Configuration validation
   - Database save verification

3. **OAuthTokenFactory**
   - Token expiration detection
   - Cache hit/miss behavior
   - Automatic refresh logic

4. **Get_User_Token**
   - HTTP request formation
   - Response deserialization
   - Error handling

## Migration Notes

### Files Moved from FeeBayOAuthConnection
1. `OAuthTokenFactory.cs` → `FeeBayOAuth.TokenFactory\OAuthTokenFactory.cs`
2. `NewRefreshTokenManager.cs` → `FeeBayOAuth.TokenFactory\OAuth2\RefreshTokenManager.cs`
3. `Get_User_Token.cs` → `FeeBayOAuth.TokenFactory\Calls\Get_User_Token.cs`
4. Response DTOs → `FeeBayOAuth.TokenFactory\DTO\Response\`
5. `OAuthApiResponse.cs` → `FeeBayOAuth.TokenFactory\Models\OAuthApiResponse.cs`

### New Files Created
1. `OAuthHttpHelpers.cs` - Extracted duplicate utility methods
2. `README.md` - Complete library documentation

### Files Removed
1. `FeeBayOAuthConnection\OAuthTokenFactory.cs` (moved)
2. `FeeBayOAuthConnection\OAuth2\NewRefreshTokenManager.cs` (moved & refactored)

## Version History

### v1.0 - Initial Extract
- Moved `OAuthTokenFactory` to standalone library
- Added comprehensive documentation

### v1.1 - Complete OAuth Lifecycle
- Added `RefreshTokenManager` (initial authorization)
- Extracted `OAuthHttpHelpers` utility class
- Refactored duplicate code
- Updated README with complete OAuth flow examples
