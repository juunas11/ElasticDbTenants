using ElasticDbTenants.CatalogDb;
using ElasticDbTenants.CatalogDb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElasticDbTenants.App.Pages
{
    public class IndexModel : PageModel
    {
        private readonly CatalogDbContext _catalogDbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _deleteTenantWorkflowStartUrl;

        public IndexModel(
            CatalogDbContext catalogDbContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _catalogDbContext = catalogDbContext;
            _httpClientFactory = httpClientFactory;
            _deleteTenantWorkflowStartUrl = configuration["DeleteTenantWorkflowStartUrl"];
        }

        public List<Tenant> Tenants { get; set; }

        [BindProperty]
        public Guid? TenantIdToDelete { get; set; }

        public async Task OnGetAsync()
        {
            Tenants = await _catalogDbContext.Tenants
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            if (!TenantIdToDelete.HasValue)
            {
                return BadRequest();
            }

            var workflowClient = _httpClientFactory.CreateClient(HttpClients.DurableWorkflow);

            var response = await workflowClient.PostAsync(
                _deleteTenantWorkflowStartUrl,
                new StringContent(
                    JsonSerializer.Serialize(new DeleteTenantInputModel
                    {
                        TenantId = TenantIdToDelete.Value,
                    }),
                    Encoding.UTF8,
                    "application/json"));
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                throw new Exception("Failed to start workflow");
            }

            return RedirectToPage("DeleteStatus", new { tenantId = TenantIdToDelete.Value.ToString() });
        }
    }

    public class DeleteTenantInputModel
    {
        public Guid TenantId { get; set; }
    }
}
