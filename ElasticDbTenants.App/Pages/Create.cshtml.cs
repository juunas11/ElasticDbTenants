using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ElasticDbTenants.CatalogDb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace ElasticDbTenants.App.Pages
{
    public class CreateModel : PageModel
    {
        private readonly CatalogDbContext _catalogDbContext;
        private readonly HttpClient _workflowClient;
        private readonly string _createTenantWorkflowStartUrl;

        public CreateModel(
            CatalogDbContext catalogDbContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _catalogDbContext = catalogDbContext;
            _workflowClient = httpClientFactory.CreateClient(HttpClients.DurableWorkflow);
            _createTenantWorkflowStartUrl = configuration["CreateTenantWorkflowStartUrl"];
        }

        [BindProperty, Required]
        public string TenantName { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var tenantId = Guid.NewGuid();

            _catalogDbContext.Tenants
                .Add(new CatalogDb.Models.Tenant
                {
                    Id = tenantId,
                    Name = TenantName,
                    CreationStatus = CatalogDb.Models.TenantCreationStatus.Started,
                    ConnectionString = "",
                    ServerName = "",
                    DatabaseName = ""
                });
            await _catalogDbContext.SaveChangesAsync();

            // Start workflow
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

            return RedirectToPage("CreateStatus", new { tenantId = tenantId.ToString() });
        }
    }

    public class CreateTenantInputModel
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; }
    }
}


