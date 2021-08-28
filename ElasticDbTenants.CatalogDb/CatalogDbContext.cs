using ElasticDbTenants.CatalogDb.Models;
using Microsoft.EntityFrameworkCore;

namespace ElasticDbTenants.CatalogDb
{
    public class CatalogDbContext : DbContext
    {
        public CatalogDbContext(
            DbContextOptions<CatalogDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }
    }
}
