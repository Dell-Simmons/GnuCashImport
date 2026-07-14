using FeeBayConnectionTester.DTO;
using Stripe;
namespace FeeBayConnectionTester.Services.Stripe
{
    public interface IStripeTransactionProcessor
    {
        List<ToGnuCash> ReformatStripeForGnuCash(List<BalanceTransaction> incomingRecords);
        Task<List<ToGnuCash>> ReformatStripeForGnuCashAsync(List<BalanceTransaction> incomingRecords);
    }
}
