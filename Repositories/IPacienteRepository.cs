using NutriFlow.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NutriFlow.Repositories
{
    public interface IPacienteRepository : IRepository<Paciente>
    {
        Task<List<Paciente>> GetPacientesByUsuarioIdAsync(int usuarioId);
        Task<Paciente?> GetPacienteCompletoAsync(int id, int usuarioId);
        Task<Paciente?> GetPacienteCompletoSemUsuarioAsync(int id);
        Task<List<Paciente>> GetPacientesRecentesAsync(int usuarioId, int count);
    }
}
