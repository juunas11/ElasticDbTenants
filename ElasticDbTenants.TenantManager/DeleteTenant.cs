using ElasticDbTenants.CatalogDb;
using Microsoft.Azure.Management.Sql;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ElasticDbTenants.TenantManager
{
    public class DeleteTenant
    {
        private readonly SqlManagementClient _sqlManagementClient;
        private readonly CatalogDbContext _catalogDbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public DeleteTenant(
            SqlManagementClient sqlManagementClient,
            CatalogDbContext catalogDbContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _sqlManagementClient = sqlManagementClient;
            _catalogDbContext = catalogDbContext;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [FunctionName("DeleteTenant")]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var input = context.GetInput<DeleteTenantInputModel>();

            try
            {
                // Delete the tenant database
                await context.CallActivityAsync("DeleteTenant_DeleteDb", input);

                // Delete tenant from Catalog DB
                await context.CallActivityAsync("DeleteTenant_DeleteFromCatalog", input);

                // Notify the App back-end
                await context.CallActivityAsync("DeleteTenant_NotifyComplete", input);
            }
            catch
            {
                await context.CallActivityAsync("DeleteTenant_NotifyFailed", input);
                throw;
            }
        }

        [FunctionName("DeleteTenant_DeleteDb")]
        public async Task DeleteDb(
            [ActivityTrigger] DeleteTenantInputModel model)
        {
            var tenant = await _catalogDbContext.Tenants
                .AsNoTracking()
                .SingleOrDefaultAsync(t => t.Id == model.TenantId);
            if (tenant is null)
            {
                // Tenant has already been deleted
                return;
            }

            var rgName = _configuration["ElasticPoolResourceGroup"];
            var serverName = tenant.ServerName;
            var dbName = tenant.DatabaseName;

            await _sqlManagementClient.Databases.DeleteAsync(rgName, serverName, dbName);
        }

        [FunctionName("DeleteTenant_DeleteFromCatalog")]
        public async Task DeleteFromCatalog(
            [ActivityTrigger] DeleteTenantInputModel model)
        {
            var tenant = await _catalogDbContext.Tenants
                .SingleOrDefaultAsync(t => t.Id == model.TenantId);
            if (tenant is null)
            {
                // Already deleted
                return;
            }

            _catalogDbContext.Tenants.Remove(tenant);

            await _catalogDbContext.SaveChangesAsync();
        }

        [FunctionName("DeleteTenant_NotifyComplete")]
        public async Task NotifyComplete(
            [ActivityTrigger] DeleteTenantInputModel model)
        {
            var client = _httpClientFactory.CreateClient(HttpClients.AppApi);
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_configuration["AppBackendBaseUrl"]}/api/notifications/tenants/{model.TenantId}/deleted");
            await client.SendAsync(request);
            // TODO: Check response
        }

        [FunctionName("DeleteTenant_NotifyFailed")]
        public async Task NotifyFailed(
            [ActivityTrigger] DeleteTenantInputModel model)
        {
            var client = _httpClientFactory.CreateClient(HttpClients.AppApi);
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_configuration["AppBackendBaseUrl"]}/api/notifications/tenants/{model.TenantId}/deleteFailed");
            await client.SendAsync(request);
            // TODO: Check response
        }

        [FunctionName("DeleteTenant_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var input = await req.Content.ReadAsAsync<DeleteTenantInputModel>();
            string instanceId = await starter.StartNewAsync("DeleteTenant", input);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }

    public class DeleteTenantInputModel
    {
        public Guid TenantId { get; set; }
    }
}
