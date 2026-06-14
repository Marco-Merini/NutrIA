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
                .AsNoTracking()
                .Include(p => p.Refeicoes)
                .Where(p => p.PacienteId == pacienteId)
                .OrderByDescending(p => p.DataCriacao)
                .ToListAsync();
        }

        public async Task<PlanoDieta?> GetPlanoCompletoAsync(int id)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.Refeicoes)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<int> GetPlanosCountPorUsuarioAsync(int usuarioId)
        {
            return await _dbSet
                .Where(p => p.Paciente != null && p.Paciente.UsuarioId == usuarioId)
                .CountAsync();
        }

        public async Task<(List<PlanoDieta> Items, int TotalCount)> GetPlanosFiltradosAsync(int usuarioId, PlanoDietaFilter filter)
        {
            var query = BuildBaseQuery(usuarioId);

            query = ApplyFilters(query, filter);
            query = ApplyOrdering(query, filter);

            int totalCount = await query.CountAsync();

            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        private IQueryable<PlanoDieta> BuildBaseQuery(int usuarioId)
        {
            return _dbSet.AsNoTracking()
                .Include(p => p.Refeicoes)
                .Include(p => p.Paciente)
                .Where(p => p.Paciente != null && p.Paciente.UsuarioId == usuarioId);
        }

        private IQueryable<PlanoDieta> ApplyFilters(IQueryable<PlanoDieta> query, PlanoDietaFilter filter)
        {
            if (filter.PacienteId.HasValue)
            {
                query = query.Where(p => p.PacienteId == filter.PacienteId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Nome))
            {
                query = query.Where(p =>
                    p.Titulo != null &&
                    EF.Functions.Like(p.Titulo, $"%{filter.Nome}%"));
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

            return query;
        }

        private IQueryable<PlanoDieta> ApplyOrdering(IQueryable<PlanoDieta> query, PlanoDietaFilter filter)
        {
            bool isDesc = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(filter.SortBy))
            {
                return query.OrderByDescending(p => p.DataCriacao);
            }

            return filter.SortBy.ToLower() switch
            {
                "titulo" => isDesc
                    ? query.OrderByDescending(p => p.Titulo)
                    : query.OrderBy(p => p.Titulo),

                "datacriacao" => isDesc
                    ? query.OrderByDescending(p => p.DataCriacao)
                    : query.OrderBy(p => p.DataCriacao),

                _ => query.OrderByDescending(p => p.DataCriacao)
            };
        }
    }
}
