using DiscountCodeApplication.DB;
using Serilog;

namespace DiscountCodeApplication.UnitOfWork;

public class UnitOfWork(DiscountCodeContext context) : IUnitOfWork
{
    public async Task CompleteAsync()
    {
        Log.Information("UnitOfWork.CompleteAsync called: saving changes to the database.");
        try
        {
            await context.SaveChangesAsync();
            Log.Information("UnitOfWork.CompleteAsync succeeded: changes saved.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "UnitOfWork.CompleteAsync failed: error saving changes to the database.");
            throw;
        }
    }
}