using System.Collections.Generic;
using System.Threading.Tasks;
using AITalentHub.Models;

namespace AITalentHub.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(AuditLog auditLog);
        Task<List<AuditLog>> GetRecentAsync(int limit = 100);
    }
}
