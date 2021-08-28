using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ElasticDbTenants.CatalogDb;
using ElasticDbTenants.CatalogDb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ElasticDbTenants.App.Pages
{
    public class CreateStatusModel : PageModel
    {
        private readonly CatalogDbContext _catalogDbContext;
        private readonly HttpClient _workflowClient;
        private readonly string _createTenantWorkflowStartUrl;

        public CreateStatusModel(
            CatalogDbContext catalogDbContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _catalogDbContext = catalogDbContext;
            _workflowClient = httpClientFactory.CreateClient(HttpClients.DurableWorkflow);
            _createTenantWorkflowStartUrl = configuration["CreateTenantWorkflowStartUrl"];
        }

        public Guid TenantId { get; set; }
        public string TenantName { get; set; }
        public TenantCreationStatus TenantCreationStatus { get; set; }

        public async Task OnGetAsync(Guid tenantId)
        {
            var tenant = await _catalogDbContext.Tenants
                .AsNoTracking()
                .SingleAsync(t => t.Id == tenantId);
            TenantId = tenantId;
            TenantName = tenant.Name;
            TenantCreationStatus = tenant.CreationStatus;
        }

        public async Task<IActionResult> OnPostRetryAsync(Guid tenantId)
        {
            var tenant = await _catalogDbContext.Tenants
                .SingleAsync(t => t.Id == tenantId);

            tenant.CreationStatus = TenantCreationStatus.Started;
            await _catalogDbContext.SaveChangesAsync();

            var response = await _workflowClient.PostAsync(
                _createTenantWorkflowStartUrl,
                new StringContent(
                    JsonSerializer.Serialize(new CreateTenantInputModel
                    {
                        TenantId = tenantId,
                        TenantName = TenantName,
                    }),
                    Encoding.UTF8,
                    "application/json"));
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                throw new Exception("Failed to start workflow");
            }

            return RedirectToPage(new
            {
                tenantId = tenantId.ToString()
            });
        }
    }
}


