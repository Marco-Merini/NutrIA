using NutriFlow.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NutriFlow.Services
{
    public interface IProgressoService
    {
        Task<List<Progresso>> GetProgressosPorPacienteAsync(int pacienteId);
        Task<int> GetProgressosCountPorUsuarioAsync(int usuarioId);
        Task<Progresso?> GetProgressoByIdAsync(int id);
        Task<bool> AddProgressoAsync(Progresso progresso);
        Task<bool> DeleteProgressoAsync(int id);
    }
}
