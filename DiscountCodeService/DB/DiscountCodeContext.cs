using DiscountCodeServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscountCodeServer.DB
{
    public class DiscountCodeContext : DbContext
    {
        public DiscountCodeContext(DbContextOptions<DiscountCodeContext> options) : base(options)
        {
        }

        public DbSet<DiscountCode> DiscountCodes { get; set; }
    }
}