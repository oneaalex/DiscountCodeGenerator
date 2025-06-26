using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;


namespace DiscountCodeServer.DB
{
    public class DiscountCodeContextFactory : IDesignTimeDbContextFactory<DiscountCodeContext>
    {
        public DiscountCodeContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DiscountCodeContext>();
            optionsBuilder.UseSqlServer("Server=localhost;Database=MyDb;Trusted_Connection=True;");

            return new DiscountCodeContext(optionsBuilder.Options);
        }
    }
}