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
                .AsNoTracking()
                .Include(s => s.Paciente)
                .Where(s => s.Paciente != null && s.Paciente.UsuarioId == usuarioId)
                .OrderByDescending(s => s.DataSessao)
                .ToListAsync();
        }

        public async Task<List<Sessao>> GetSessoesRecentesPorUsuarioAsync(int usuarioId, int count)
        {
            return await _dbSet
                .AsNoTracking()
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

        public async Task<(List<Sessao> Items, int TotalCount)> GetSessoesFiltradasAsync(int usuarioId, SessaoFilter filter)
        {
            var query = _dbSet.AsNoTracking()
                .Include(s => s.Paciente)
                .Where(s => s.Paciente != null && s.Paciente.UsuarioId == usuarioId);

            if (filter.PacienteId.HasValue)
            {
                query = query.Where(s => s.PacienteId == filter.PacienteId.Value);
            }

            if (filter.DataInicio.HasValue)
            {
                query = query.Where(s => s.DataCriacao >= filter.DataInicio.Value);
            }

            if (filter.DataFim.HasValue)
            {
                var endOfDay = filter.DataFim.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(s => s.DataCriacao <= endOfDay);
            }

            // Ordenação dinâmica
            if (!string.IsNullOrWhiteSpace(filter.SortBy))
            {
                bool isDesc = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
                switch (filter.SortBy.ToLower())
                {
                    case "datasessao":
                        query = isDesc ? query.OrderByDescending(s => s.DataSessao) : query.OrderBy(s => s.DataSessao);
                        break;
                    case "datacriacao":
                    default:
                        query = isDesc ? query.OrderByDescending(s => s.DataCriacao) : query.OrderBy(s => s.DataCriacao);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(s => s.DataSessao);
            }

            int totalCount = await query.CountAsync();

            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
