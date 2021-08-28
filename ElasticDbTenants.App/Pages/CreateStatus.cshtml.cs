using System;
using System.Threading.Tasks;
using ElasticDbTenants.CatalogDb;
using ElasticDbTenants.CatalogDb.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ElasticDbTenants.App.Pages
{
    public class CreateStatusModel : PageModel
    {
        private readonly CatalogDbContext _catalogDbContext;

        public CreateStatusModel(CatalogDbContext catalogDbContext)
        {
            _catalogDbContext = catalogDbContext;
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
    }
}


