using NutriFlow.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NutriFlow.Services
{
    public interface IPlanoDietaService
    {
        Task<List<PlanoDieta>> GetPlanosPorPacienteAsync(int pacienteId);
        Task<PlanoDieta?> GetPlanoCompletoAsync(int id);
        Task<int> GetPlanosCountPorUsuarioAsync(int usuarioId);
        Task<bool> AddPlanoDietaAsync(PlanoDieta plano);
        Task<bool> DeletePlanoDietaAsync(int id);
    }
}
