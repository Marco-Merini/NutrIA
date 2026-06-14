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
                .AsNoTracking()
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

        public async Task<(List<Progresso> Items, int TotalCount)> GetProgressosFiltradosAsync(int usuarioId, ProgressoFilter filter)
        {
            var query = _dbSet.AsNoTracking()
                .Include(p => p.Paciente)
                .Where(p => p.Paciente != null && p.Paciente.UsuarioId == usuarioId);

            if (filter.PacienteId.HasValue)
            {
                query = query.Where(p => p.PacienteId == filter.PacienteId.Value);
            }

            if (filter.DataInicio.HasValue)
            {
                query = query.Where(p => p.DataCriacao >= filter.DataInicio.Value);
            }

            if (filter.DataFim.HasValue)
            {
                var endOfDay = filter.DataFim.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(p => p.DataCriacao <= endOfDay);
            }

            // Ordenação dinâmica
            if (!string.IsNullOrWhiteSpace(filter.SortBy))
            {
                bool isDesc = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
                switch (filter.SortBy.ToLower())
                {
                    case "dataregistro":
                        query = isDesc ? query.OrderByDescending(p => p.DataRegistro) : query.OrderBy(p => p.DataRegistro);
                        break;
                    case "datacriacao":
                    default:
                        query = isDesc ? query.OrderByDescending(p => p.DataCriacao) : query.OrderBy(p => p.DataCriacao);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(p => p.DataRegistro);
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
