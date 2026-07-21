using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AITalentHub.Data;
using AITalentHub.Models;

namespace AITalentHub.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;

        public AuditLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(AuditLog auditLog)
        {
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetRecentAsync(int limit = 100)
        {
            return await _context.AuditLogs
                                 .OrderByDescending(a => a.Timestamp)
                                 .Take(limit)
                                 .ToListAsync();
        }
    }
}
