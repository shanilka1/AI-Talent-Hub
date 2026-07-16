using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AITalentHub.Services;

namespace AITalentHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecent([FromQuery] int limit = 100)
        {
            var logs = await _auditLogService.GetRecentAsync(limit);
            return Ok(logs.Select(l => new
            {
                l.Id,
                l.Timestamp,
                l.UserId,
                l.UserEmail,
                l.UserRole,
                l.Action,
                l.Entity,
                l.EntityId,
                l.Details
            }));
        }
    }
}
