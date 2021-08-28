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

            // Delete the tenant database
            await context.CallActivityAsync("DeleteTenant_DeleteDb", input);

            // Notify the App back-end
            await context.CallActivityAsync("DeleteTenant_NotifyComplete", input);
        }

        [FunctionName("DeleteTenant_DeleteDb")]
        public async Task DeleteDb(
            [ActivityTrigger] DeleteTenantInputModel model)
        {
            var tenant = await _catalogDbContext.Tenants
                .AsNoTracking()
                .SingleAsync(t => t.Id == model.TenantId);

            var rgName = _configuration["ElasticPoolResourceGroup"];
            var serverName = tenant.ServerName;
            var dbName = tenant.DatabaseName;

            await _sqlManagementClient.Databases.DeleteAsync(rgName, serverName, dbName);
        }

        [FunctionName("DeleteTenant_NotifyComplete")]
        public async Task NotifyComplete(
            [ActivityTrigger] DeleteTenantInputModel model)
        {
            var client = _httpClientFactory.CreateClient(HttpClients.AppApi);
            await client.DeleteAsync(
                $"{_configuration["AppBackendBaseUrl"]}/api/tenants/{model.TenantId}");
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
