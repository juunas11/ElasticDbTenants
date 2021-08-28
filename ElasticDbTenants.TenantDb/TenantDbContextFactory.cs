using ElasticDbTenants.Db.Common;
using Microsoft.EntityFrameworkCore;

namespace ElasticDbTenants.TenantDb
{
    public class TenantDbContextFactory
    {
        private readonly EfAzureAdInterceptor _authenticationInterceptor;

        public TenantDbContextFactory(EfAzureAdInterceptor authenticationInterceptor)
        {
            _authenticationInterceptor = authenticationInterceptor;
        }

        public TenantDbContext CreateTenantDbContext(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseSqlServer(connectionString)
                .AddInterceptors(_authenticationInterceptor);

            return new TenantDbContext(optionsBuilder.Options);
        }
    }
}
