using NutriFlow.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NutriFlow.Services
{
    public interface ISessaoService
    {
        Task<List<Sessao>> GetSessoesPorUsuarioAsync(int usuarioId);
        Task<List<Sessao>> GetSessoesRecentesPorUsuarioAsync(int usuarioId, int count);
        Task<int> GetSessoesCountPorUsuarioAsync(int usuarioId);
        Task<Sessao?> GetSessaoByIdAsync(int id);
        Task<bool> AddSessaoAsync(Sessao sessao);
        Task<bool> UpdateSessaoAsync(Sessao sessao);
        Task<bool> DeleteSessaoAsync(int id);
        Task<PaginatedResult<SessaoResponseDto>> GetSessoesFiltradasAsync(int usuarioId, SessaoFilter filter);
    }
}
