using System.Threading.Tasks;

namespace FeeBayFinances
{
    public interface IEbayTokenService
    {
        Task<string> GetValidUserAccessTokenAsync();
    }
}