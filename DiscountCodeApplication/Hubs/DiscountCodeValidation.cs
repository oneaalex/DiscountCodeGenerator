
namespace DiscountCodeApplication.Hubs
{
    public static class DiscountCodeValidation
    {
        public static string? ValidateGenerateCodeInput(ushort count, byte length)
        {
            if (count < 1 || count > 2000)
                return "Count must be between 1 and 2000.";
            if (length < 7 || length > 8)
                return "Length must be 7 or 8.";
            return null;
        }

        internal static string? ValidateUseCodeInput(string code)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length > 8)
            {
                return "Code must be 8 characters or fewer.";
            }
            return null;
        }
    }
}
