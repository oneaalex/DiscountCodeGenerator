namespace DiscountCodeApplication.Services.Interfaces
{
    public interface IDiscountCodeService
    {
        Task<bool> GenerateAndAddCodesAsync(ushort count, byte length);
        Task<byte> UseCodeAsync(string code);
        Task<List<string>> GetAllCodesAsync();
        Task<List<string>> GetMostRecentCodesAsync(int count = 10);
    }
}
