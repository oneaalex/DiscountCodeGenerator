using DiscountCodeApplication.Services.Interfaces;

namespace DiscountCodeApplication.Services;

public class DiscountCodeGenerator : IDiscountCodeGenerator
{
    private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public List<string> GenerateCodes(ushort count, byte length, List<string> existingCodes)
    {
        if (count == 0 || count > 2000)
        {
            throw new ArgumentException("Count must be between 1 and 2000", nameof(count));
        }

        if (length < 7 || length > 8)
        {
            throw new ArgumentException("Length must be between 7 and 8 characters", nameof(length));
        }

        HashSet<string> uniqueCodes = new(existingCodes, StringComparer.Ordinal);
        List<string> newCodes = new(count);
        int attempts = 0;
        int maxAttempts = count * 50; // Adjusted to avoid infinite loops for small counts

        while (newCodes.Count < count)
        {
            attempts++;
            if (attempts > maxAttempts)
            {
                throw new Exception("Could not generate enough unique codes within attempt limit.");
            }

            string code = GenerateRandomCode(length);

            if (uniqueCodes.Add(code))
            {
                newCodes.Add(code);
            }
        }

        return newCodes;
    }

    private static string GenerateRandomCode(byte length)
    {
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = Characters[Random.Shared.Next(Characters.Length)];
        }
        return new string(chars);
    }
}
