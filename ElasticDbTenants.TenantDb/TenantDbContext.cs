using ElasticDbTenants.TenantDb.Models;
using Microsoft.EntityFrameworkCore;

namespace ElasticDbTenants.TenantDb
{
    public class TenantDbContext : DbContext
    {
        public TenantDbContext(
            DbContextOptions<TenantDbContext> options)
            : base(options)
        {
        }

        public DbSet<TodoItem> TodoItems { get; set; }
    }
}
