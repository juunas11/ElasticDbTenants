using System;
using System.Threading.Tasks;
using ElasticDbTenants.CatalogDb;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ElasticDbTenants.App.Pages
{
    public class DeleteStatusModel : PageModel
    {
        private readonly CatalogDbContext _catalogDbContext;

        public DeleteStatusModel(
            CatalogDbContext catalogDbContext)
        {
            _catalogDbContext = catalogDbContext;
        }

        public Guid TenantId { get; set; }
        public bool TenantExists { get; set; }

        public async Task OnGet(Guid tenantId)
        {
            TenantId = tenantId;
            TenantExists = await _catalogDbContext.Tenants
                .AnyAsync(t => t.Id == tenantId);
        }
    }
}
