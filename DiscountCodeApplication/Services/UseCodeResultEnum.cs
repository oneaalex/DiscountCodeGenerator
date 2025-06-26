namespace DiscountCodeApplication.Services
{
    public enum UseCodeResultEnum : byte
    {
        Success = 0,
        Failure = 1,      // Not found
        AlreadyUsed = 2,
        Expired = 3,
        Inactive = 4,
        Deleted = 5,
        Exception = 6
    }
}
