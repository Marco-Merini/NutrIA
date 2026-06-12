using NutriFlow.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NutriFlow.Repositories
{
    public interface IAuditLogRepository : IRepository<AuditLog>
    {
        Task<List<AuditLog>> GetLogsPorPacienteAsync(int pacienteId, string userId, int limit);
        Task<List<AuditLog>> GetLogsPorUsuarioAsync(string userId, int limit);
        Task<List<AuditLog>> GetTodosLogsPorUsuarioAsync(string userId);
    }
}
