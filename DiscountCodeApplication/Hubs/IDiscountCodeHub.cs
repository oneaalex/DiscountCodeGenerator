namespace DiscountCodeApplication.Hubs
{
    public interface IDiscountCodeHub
    {
        public interface IDiscountCodeHub
        {
            Task<bool> GenerateCode(ushort count, byte length);
            Task<byte> UseCode(string code);
        }
    }
}