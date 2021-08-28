using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ElasticDbTenants.CatalogDb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ElasticDbTenants.App.Pages
{
    public class DeleteStatusModel : PageModel
    {
        private readonly CatalogDbContext _catalogDbContext;
        private readonly HttpClient _workflowClient;
        private readonly string _deleteTenantWorkflowStartUrl;

        public DeleteStatusModel(
            CatalogDbContext catalogDbContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _catalogDbContext = catalogDbContext;
            _workflowClient = httpClientFactory.CreateClient(HttpClients.DurableWorkflow);
            _deleteTenantWorkflowStartUrl = configuration["DeleteTenantWorkflowStartUrl"];
        }

        public Guid TenantId { get; set; }
        public bool TenantExists { get; set; }

        public async Task OnGet(Guid tenantId)
        {
            TenantId = tenantId;
            TenantExists = await _catalogDbContext.Tenants
                .AnyAsync(t => t.Id == tenantId);
        }

        public async Task<IActionResult> OnPostRetryAsync(Guid tenantId)
        {
            var response = await _workflowClient.PostAsync(
                _deleteTenantWorkflowStartUrl,
                new StringContent(
                    JsonSerializer.Serialize(new DeleteTenantInputModel
                    {
                        TenantId = tenantId,
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
