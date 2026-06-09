using Microsoft.EntityFrameworkCore;
using NutriFlow.Data;
using NutriFlow.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NutriFlow.Repositories
{
    public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<AuditLog>> GetLogsPorPacienteAsync(int pacienteId, string userId, int limit)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(l => l.PatientId == pacienteId && l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetLogsPorUsuarioAsync(string userId, int limit)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetTodosLogsPorUsuarioAsync(string userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .ToListAsync();
        }
    }
}
