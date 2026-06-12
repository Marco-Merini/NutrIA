using NutriFlow.Models;

namespace NutriFlow.Services
{
    public interface IPacienteService
    {
        Task<List<Paciente>> GetPacientesByUsuarioIdAsync(int usuarioId);
        Task<Paciente?> GetPacienteByIdAsync(int id, int usuarioId);
        Task<List<Paciente>> GetPacientesRecentesAsync(int usuarioId, int count);
        Task<bool> AddPacienteAsync(Paciente paciente);
        Task<bool> UpdatePacienteAsync(Paciente paciente);
        Task<bool> DeletePacienteAsync(int id, int usuarioId);
    }
}
