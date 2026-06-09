using Microsoft.EntityFrameworkCore;
using NutriFlow.Data;
using NutriFlow.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NutriFlow.Repositories
{
    public class SessaoRepository : Repository<Sessao>, ISessaoRepository
    {
        public SessaoRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Sessao>> GetSessoesPorUsuarioAsync(int usuarioId)
        {
            return await _dbSet
                .Include(s => s.Paciente)
                .Where(s => s.Paciente != null && s.Paciente.UsuarioId == usuarioId)
                .OrderByDescending(s => s.DataSessao)
                .ToListAsync();
        }

        public async Task<List<Sessao>> GetSessoesRecentesPorUsuarioAsync(int usuarioId, int count)
        {
            return await _dbSet
                .Include(s => s.Paciente)
                .Where(s => s.Paciente != null && s.Paciente.UsuarioId == usuarioId)
                .OrderByDescending(s => s.DataSessao)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetSessoesCountPorUsuarioAsync(int usuarioId)
        {
            return await _dbSet
                .Where(s => s.Paciente != null && s.Paciente.UsuarioId == usuarioId)
                .CountAsync();
        }
    }
}
