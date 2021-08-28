using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElasticDbTenants.CatalogDb;
using ElasticDbTenants.TenantDb;
using ElasticDbTenants.TenantDb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ElasticDbTenants.App.Pages
{
    public class ViewTenantModel : PageModel
    {
        private readonly TenantDbContextFactory _tenantDbContextFactory;
        private readonly CatalogDbContext _catalogDbContext;

        public ViewTenantModel(
            TenantDbContextFactory tenantDbContextFactory,
            CatalogDbContext catalogDbContext)
        {
            _tenantDbContextFactory = tenantDbContextFactory;
            _catalogDbContext = catalogDbContext;
        }

        public string TenantName { get; set; }
        public List<TodoItem> Todos { get; set; }

        [BindProperty]
        public Guid TenantId { get; set; }
        [BindProperty]
        public string NewTodoText { get; set; }

        public async Task OnGetAsync(Guid tenantId)
        {
            var tenant = await _catalogDbContext.Tenants
                .AsNoTracking()
                .SingleAsync(t => t.Id == tenantId);
            TenantId = tenantId;
            TenantName = tenant.Name;

            using var tenantDbContext = _tenantDbContextFactory.CreateTenantDbContext(tenant.ConnectionString);

            Todos = await tenantDbContext.TodoItems
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var tenantConnectionString = await _catalogDbContext.Tenants
                .Where(t => t.Id == TenantId)
                .Select(t => t.ConnectionString)
                .SingleAsync();

            using var tenantDbContext = _tenantDbContextFactory.CreateTenantDbContext(tenantConnectionString);

            tenantDbContext.TodoItems.Add(new TodoItem
            {
                Text = NewTodoText,
                IsDone = false
            });
            await tenantDbContext.SaveChangesAsync();

            return RedirectToPage(new { tenantId = TenantId });
        }
    }
}
