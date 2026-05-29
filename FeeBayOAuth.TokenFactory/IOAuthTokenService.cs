using System.Threading.Tasks;

namespace FeeBayOAuth.TokenService
{
    public interface IOAuthTokenService
    {
        Task<string?> GetOAuthTokenAsync(string feeBayUserName, CancellationToken cancellationToken = default);
    }
}