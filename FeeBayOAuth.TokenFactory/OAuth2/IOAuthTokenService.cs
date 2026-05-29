using System.Threading.Tasks;

namespace FeeBayFinances
{
    public interface IOAuthTokenService
    {
        Task<string?> GetOAuthTokenAsync(string feeBayUserName, CancellationToken cancellationToken = default);
    }
}