using Azure.Core;
using Azure.Identity;
using ElasticDbTenants.CatalogDb;
using ElasticDbTenants.Db.Common;
using ElasticDbTenants.TenantDb;
using ElasticDbTenants.TenantManager.Util;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Management.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(ElasticDbTenants.TenantManager.Startup))]

namespace ElasticDbTenants.TenantManager
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = builder.GetContext().Configuration;
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                SharedTokenCacheTenantId = config["LocalDevelopmentAadTenantId"],
                VisualStudioCodeTenantId = config["LocalDevelopmentAadTenantId"],
                VisualStudioTenantId = config["LocalDevelopmentAadTenantId"]
            });
            builder.Services.AddSingleton<TokenCredential>(credential);

            var catalogDbConnectionString = config["CatalogDbConnectionString"];
            var authenticationInterceptor = new EfAzureAdInterceptor(credential);
            builder.Services.AddDbContext<CatalogDbContext>(db =>
                db.UseSqlServer(catalogDbConnectionString).AddInterceptors(authenticationInterceptor));

            builder.Services.AddSingleton(new TenantDbContextFactory(authenticationInterceptor));

            builder.Services.AddSingleton<AzureIdentityCredentials>();

            builder.Services.AddTransient(sp =>
            {
                var sqlManagementCredentials = sp.GetRequiredService<AzureIdentityCredentials>();
                return new SqlManagementClient(sqlManagementCredentials)
                {
                    SubscriptionId = config["ElasticPoolSubscriptionId"]
                };
            });

            builder.Services.AddHttpClient(HttpClients.AppApi);
        }
    }
}
