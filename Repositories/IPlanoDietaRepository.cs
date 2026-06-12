using NutriFlow.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NutriFlow.Repositories
{
    public interface IPlanoDietaRepository : IRepository<PlanoDieta>
    {
        Task<List<PlanoDieta>> GetPlanosPorPacienteAsync(int pacienteId);
        Task<PlanoDieta?> GetPlanoCompletoAsync(int id);
        Task<int> GetPlanosCountPorUsuarioAsync(int usuarioId);
    }
}
