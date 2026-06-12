using Microsoft.EntityFrameworkCore;
using NutriFlow.Data;
using NutriFlow.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NutriFlow.Repositories
{
    public class ProgressoRepository : Repository<Progresso>, IProgressoRepository
    {
        public ProgressoRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Progresso>> GetProgressosPorPacienteAsync(int pacienteId)
        {
            return await _dbSet
                .Where(p => p.PacienteId == pacienteId)
                .OrderByDescending(p => p.DataRegistro)
                .ToListAsync();
        }

        public async Task<int> GetProgressosCountPorUsuarioAsync(int usuarioId)
        {
            return await _dbSet
                .Where(p => p.Paciente != null && p.Paciente.UsuarioId == usuarioId)
                .CountAsync();
        }
    }
}
