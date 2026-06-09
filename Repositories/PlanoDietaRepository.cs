using Microsoft.EntityFrameworkCore;
using NutriFlow.Data;
using NutriFlow.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NutriFlow.Repositories
{
    public class PlanoDietaRepository : Repository<PlanoDieta>, IPlanoDietaRepository
    {
        public PlanoDietaRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<PlanoDieta>> GetPlanosPorPacienteAsync(int pacienteId)
        {
            return await _dbSet
                .Include(p => p.Refeicoes)
                .Where(p => p.PacienteId == pacienteId)
                .OrderByDescending(p => p.DataCriacao)
                .ToListAsync();
        }

        public async Task<PlanoDieta?> GetPlanoCompletoAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Refeicoes)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<int> GetPlanosCountPorUsuarioAsync(int usuarioId)
        {
            return await _dbSet
                .Where(p => p.Paciente != null && p.Paciente.UsuarioId == usuarioId)
                .CountAsync();
        }
    }
}
