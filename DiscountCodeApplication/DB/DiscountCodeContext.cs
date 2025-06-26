using DiscountCodeApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscountCodeApplication.DB
{
    public class DiscountCodeContext(DbContextOptions<DiscountCodeContext> options) : DbContext(options)
    {
        public DbSet<DiscountCode> DiscountCodes { get; set; }
    }
}