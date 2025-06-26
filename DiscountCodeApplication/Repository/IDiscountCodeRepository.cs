using DiscountCodeApplication.Models;

namespace DiscountCodeApplication.Repository
{
    public interface IDiscountCodeRepository
    {
        Task<DiscountCode> GetDiscountCodeByCodeAsync(string code);
        Task AddRangeDiscountCodeAsync(IEnumerable<DiscountCode> codes);
        Task UpdateDiscountCodeAsync(DiscountCode code);
        Task DeleteDiscountCodeAsync(DiscountCode code);
        Task<List<DiscountCode>> GetRecentDiscountCodesAsync();
        Task<List<string>> GetAllCodesAsync();
        Task<List<string>> GetMostRecentCodesAsync(int count = 10);
        Task PreloadDiscountCodeCachesAsync();
    }
}