using Microsoft.EntityFrameworkCore;
using NutriFlow.Data;
using NutriFlow.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NutriFlow.Repositories
{
    public class PacienteRepository : Repository<Paciente>, IPacienteRepository
    {
        public PacienteRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Paciente>> GetPacientesByUsuarioIdAsync(int usuarioId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(p => p.UsuarioId == usuarioId)
                .OrderBy(p => p.Nome)
                .ToListAsync();
        }

        public async Task<Paciente?> GetPacienteCompletoAsync(int id, int usuarioId)
        {
            return await _dbSet
                .Include(p => p.Sessoes)
                .Include(p => p.Progressos)
                .Include(p => p.PlanosDieta)
                    .ThenInclude(pd => pd.Refeicoes)
                .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuarioId);
        }

        public async Task<Paciente?> GetPacienteCompletoSemUsuarioAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Sessoes)
                .Include(p => p.Progressos)
                .Include(p => p.PlanosDieta)
                    .ThenInclude(pd => pd.Refeicoes)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Paciente>> GetPacientesRecentesAsync(int usuarioId, int count)
        {
            return await _dbSet
                .Where(p => p.UsuarioId == usuarioId)
                .OrderByDescending(p => p.DataCadastro)
                .Take(count)
                .ToListAsync();
        }
    }
}
