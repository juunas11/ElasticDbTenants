using ElasticDbTenants.App.Hubs;
using ElasticDbTenants.CatalogDb;
using ElasticDbTenants.CatalogDb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace ElasticDbTenants.App.Controllers
{
    [Route("api/tenants")]
    public class TenantsController : ControllerBase
    {
        private readonly CatalogDbContext _catalogDbContext;
        private readonly IHubContext<TenantCreateHub> _createHubContext;
        private readonly IHubContext<TenantDeleteHub> _deleteHubContext;

        public TenantsController(
            CatalogDbContext catalogDbContext,
            IHubContext<TenantCreateHub> createHubContext,
            IHubContext<TenantDeleteHub> deleteHubContext)
        {
            _catalogDbContext = catalogDbContext;
            _createHubContext = createHubContext;
            _deleteHubContext = deleteHubContext;
        }

        [HttpPut("{tenantId}/createStatus")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid tenantId,
            [FromBody] TenantCreateStatusUpdateModel model)
        {
            var tenant = await _catalogDbContext.Tenants
                .SingleOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return NotFound();
            }

            tenant.CreationStatus = model.Status;
            await _catalogDbContext.SaveChangesAsync();

            await _createHubContext.Clients.All
                .SendAsync("statusUpdated", tenantId.ToString(), tenant.CreationStatus.ToString());

            return NoContent();
        }

        [HttpDelete("{tenantId}")]
        public async Task<IActionResult> Delete([FromRoute] Guid tenantId)
        {
            var tenant = await _catalogDbContext.Tenants
                .SingleOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return NotFound();
            }

            _catalogDbContext.Tenants.Remove(tenant);
            await _catalogDbContext.SaveChangesAsync();

            await _deleteHubContext.Clients.All
                .SendAsync("tenantDeleted", tenantId.ToString());

            return NoContent();
        }
    }

    public class TenantCreateStatusUpdateModel
    {
        public TenantCreationStatus Status { get; set; }
    }
}
