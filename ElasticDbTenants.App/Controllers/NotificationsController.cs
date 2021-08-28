using ElasticDbTenants.App.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace ElasticDbTenants.App.Controllers
{
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly IHubContext<TenantCreateHub> _createHubContext;
        private readonly IHubContext<TenantDeleteHub> _deleteHubContext;

        public NotificationsController(
            IHubContext<TenantCreateHub> createHubContext,
            IHubContext<TenantDeleteHub> deleteHubContext)
        {
            _createHubContext = createHubContext;
            _deleteHubContext = deleteHubContext;
        }

        [HttpPost("tenants/{tenantId}/created")]
        public async Task<IActionResult> TenantCreated(
            [FromRoute] Guid tenantId)
        {
            await _createHubContext.Clients.All
                .SendAsync("tenantCreated", tenantId.ToString());

            return NoContent();
        }

        [HttpPost("tenants/{tenantId}/createFailed")]
        public async Task<IActionResult> TenantCreateFailed(
            [FromRoute] Guid tenantId)
        {
            await _createHubContext.Clients.All
                .SendAsync("tenantCreateFailed", tenantId.ToString());

            return NoContent();
        }

        [HttpPost("tenants/{tenantId}/deleted")]
        public async Task<IActionResult> TenantDeleted(
            [FromRoute] Guid tenantId)
        {
            await _deleteHubContext.Clients.All
                .SendAsync("tenantDeleted", tenantId.ToString());

            return NoContent();
        }

        [HttpPost("tenants/{tenantId}/deleteFailed")]
        public async Task<IActionResult> TenantDeleteFailed(
            [FromRoute] Guid tenantId)
        {
            await _deleteHubContext.Clients.All
                .SendAsync("tenantDeleteFailed", tenantId.ToString());

            return NoContent();
        }
    }
}
