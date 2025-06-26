namespace DiscountCodeApplication.Services.Interfaces;

public interface IDiscountCodeGenerator
{
    List<string> GenerateCodes(ushort count, byte length, List<string> existingCodes);
}
