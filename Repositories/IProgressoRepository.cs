using NutriFlow.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NutriFlow.Repositories
{
    public interface IProgressoRepository : IRepository<Progresso>
    {
        Task<List<Progresso>> GetProgressosPorPacienteAsync(int pacienteId);
        Task<int> GetProgressosCountPorUsuarioAsync(int usuarioId);
        Task<(List<Progresso> Items, int TotalCount)> GetProgressosFiltradosAsync(int usuarioId, ProgressoFilter filter);
    }
}
