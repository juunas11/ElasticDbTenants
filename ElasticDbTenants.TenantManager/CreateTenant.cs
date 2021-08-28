using System;
using System.Net.Http;
using System.Threading.Tasks;
using ElasticDbTenants.CatalogDb;
using ElasticDbTenants.TenantDb;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Sql;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ElasticDbTenants.TenantManager
{
    public class CreateTenant
    {
        private readonly SqlManagementClient _sqlManagementClient;
        private readonly CatalogDbContext _catalogDbContext;
        private readonly TenantDbContextFactory _tenantDbContextFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public CreateTenant(
            SqlManagementClient sqlManagementClient,
            CatalogDbContext catalogDbContext,
            TenantDbContextFactory tenantDbContextFactory,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _sqlManagementClient = sqlManagementClient;
            _catalogDbContext = catalogDbContext;
            _tenantDbContextFactory = tenantDbContextFactory;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [FunctionName("CreateTenant")]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var input = context.GetInput<CreateTenantInputModel>();

            try
            {
                // Create tenant database in elastic pool
                var createDbResult = await context.CallActivityAsync<CreateDatabaseResult>(
                    "CreateTenant_CreateDb", input);

                // Create tables
                await context.CallActivityAsync("CreateTenant_InitializeDb", createDbResult);

                // Give the App back-end access to tenant DB
                await context.CallActivityAsync("CreateTenant_GrantBackendAccessToDb", createDbResult);

                // Save tenant DB details in catalog
                await context.CallActivityAsync("CreateTenant_SaveDbDetails", createDbResult);

                // Notify creation complete to App
                var notifyCompletionModel = new CreateTenantNotifyCompleteModel
                {
                    Input = input,
                    CreateDbResult = createDbResult
                };
                await context.CallActivityAsync("CreateTenant_NotifyComplete", notifyCompletionModel);
            }
            catch
            {
                await context.CallActivityAsync("CreateTenant_NotifyFailed", input);
                throw;
            }
        }

        [FunctionName("CreateTenant_CreateDb")]
        public async Task<CreateDatabaseResult> CreateDatabase(
            [ActivityTrigger] CreateTenantInputModel input)
        {
            var serverName = _configuration["ElasticPoolServerName"];
            var dbName = $"tenant-{input.TenantId}";
            await _sqlManagementClient.Databases.CreateOrUpdateAsync(
                _configuration["ElasticPoolResourceGroup"],
                serverName,
                dbName,
                new Microsoft.Azure.Management.Sql.Models.Database
                {
                    Location = _configuration["ElasticPoolRegion"],
                    Collation = "SQL_Latin1_General_CP1_CI_AS",
                    ElasticPoolId = $"/subscriptions/{_configuration["ElasticPoolSubscriptionId"]}/resourceGroups/{_configuration["ElasticPoolResourceGroup"]}/providers/Microsoft.Sql/servers/{serverName}/elasticPools/{_configuration["ElasticPoolName"]}",
                });

            return new CreateDatabaseResult
            {
                TenantId = input.TenantId,
                ConnectionString = $"Server=tcp:{serverName}.database.windows.net,1433; Initial Catalog={dbName};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
                ServerName = serverName,
                DatabaseName = dbName
            };
        }

        [FunctionName("CreateTenant_InitializeDb")]
        public async Task InitializeDatabase(
            [ActivityTrigger] CreateDatabaseResult createDatabaseResult)
        {
            using var tenantDbContext = _tenantDbContextFactory
                .CreateTenantDbContext(createDatabaseResult.ConnectionString);

            await tenantDbContext.Database.MigrateAsync();
        }

        [FunctionName("CreateTenant_GrantBackendAccessToDb")]
        public async Task GrantAccessToDatabase(
            [ActivityTrigger] CreateDatabaseResult createDatabaseResult)
        {
            using var tenantDbContext = _tenantDbContextFactory
                .CreateTenantDbContext(createDatabaseResult.ConnectionString);

            // The username can be an AAD username,
            // group name, or service principal name
            // It has to be from the same AAD tenant
            // as where the SQL server is.

            string username = _configuration["BackendUsername"];
            
            await tenantDbContext.Database.ExecuteSqlRawAsync(
$@"IF NOT EXISTS (SELECT principal_id FROM sys.database_principals WHERE name = '{username}')
CREATE USER [{username}] FROM EXTERNAL PROVIDER;
ALTER ROLE [db_datareader] ADD MEMBER [{username}];
ALTER ROLE [db_datawriter] ADD MEMBER [{username}];");
        }

        [FunctionName("CreateTenant_SaveDbDetails")]
        public async Task SaveDbDetails(
            [ActivityTrigger] CreateDatabaseResult createDatabaseResult)
        {
            var tenant = await _catalogDbContext.Tenants
                .SingleAsync(t => t.Id == createDatabaseResult.TenantId);
            tenant.ConnectionString = createDatabaseResult.ConnectionString;
            tenant.ServerName = createDatabaseResult.ServerName;
            tenant.DatabaseName = createDatabaseResult.DatabaseName;
            tenant.CreationStatus = CatalogDb.Models.TenantCreationStatus.Completed;
            await _catalogDbContext.SaveChangesAsync();
        }

        [FunctionName("CreateTenant_NotifyComplete")]
        public async Task NotifyComplete(
            [ActivityTrigger] CreateTenantNotifyCompleteModel notifyCompleteModel)
        {
            // Notification to API -> SignalR -> FE
            var client = _httpClientFactory.CreateClient(HttpClients.AppApi);
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_configuration["AppBackendBaseUrl"]}/api/notifications/tenants/{notifyCompleteModel.Input.TenantId}/created");
            await client.SendAsync(request);
            // TODO: Check response
        }

        [FunctionName("CreateTenant_NotifyFailed")]
        public async Task NotifyFailed(
            [ActivityTrigger] CreateTenantInputModel model)
        {
            var client = _httpClientFactory.CreateClient(HttpClients.AppApi);
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_configuration["AppBackendBaseUrl"]}/api/notifications/tenants/{model.TenantId}/createFailed");
            await client.SendAsync(request);
            // TODO: Check response
        }

        [FunctionName("CreateTenant_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var input = await req.Content.ReadAsAsync<CreateTenantInputModel>();
            string instanceId = await starter.StartNewAsync("CreateTenant", input);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }

    public class CreateTenantInputModel
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; }
    }

    public class CreateDatabaseResult
    {
        public Guid TenantId { get; set; }
        public string ConnectionString { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
    }

    public class CreateTenantNotifyCompleteModel
    {
        public CreateTenantInputModel Input { get; set; }
        public CreateDatabaseResult CreateDbResult { get; set; }
    }
}