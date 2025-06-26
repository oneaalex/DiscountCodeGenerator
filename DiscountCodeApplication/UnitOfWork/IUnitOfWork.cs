namespace DiscountCodeApplication.UnitOfWork;

public interface IUnitOfWork
{
    Task CompleteAsync();
}
