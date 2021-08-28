using Azure.Core;
using Azure.Identity;
using ElasticDbTenants.App.Hubs;
using ElasticDbTenants.CatalogDb;
using ElasticDbTenants.Db.Common;
using ElasticDbTenants.TenantDb;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ElasticDbTenants.App
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddControllers();

            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                SharedTokenCacheTenantId = Configuration["LocalDevelopmentAadTenantId"],
                VisualStudioCodeTenantId = Configuration["LocalDevelopmentAadTenantId"],
                VisualStudioTenantId = Configuration["LocalDevelopmentAadTenantId"]
            });
            services.AddSingleton<TokenCredential>(credential);

            var catalogDbConnectionString = Configuration["CatalogDbConnectionString"];
            var authenticationInterceptor = new EfAzureAdInterceptor(credential);
            services.AddDbContext<CatalogDbContext>(db =>
                db.UseSqlServer(catalogDbConnectionString).AddInterceptors(authenticationInterceptor));

            services.AddSingleton(new TenantDbContextFactory(authenticationInterceptor));

            services.AddHttpClient(HttpClients.DurableWorkflow);

            services.AddSignalR()
                .AddAzureSignalR(Configuration["AzureSignalrConnectionString"]);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<TenantCreateHub>("/tenantCreate");
                endpoints.MapHub<TenantDeleteHub>("/tenantDelete");
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}
