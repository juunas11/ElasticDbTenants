using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ElasticDbTenants.TenantDb
{
    /// <summary>
    /// Used with EF commands to generate
    /// migrations. They need an EF DbContext,
    /// so this enables them to work.
    /// </summary>
    public class TenantDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TenantDbContext>
    {
        public TenantDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLOCALDB;Initial Catalog=DummyTenantDb");
            return new TenantDbContext(optionsBuilder.Options);
        }
    }
}
