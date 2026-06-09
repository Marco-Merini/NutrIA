using NutriFlow.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NutriFlow.Repositories
{
    public interface ISessaoRepository : IRepository<Sessao>
    {
        Task<List<Sessao>> GetSessoesPorUsuarioAsync(int usuarioId);
        Task<List<Sessao>> GetSessoesRecentesPorUsuarioAsync(int usuarioId, int count);
        Task<int> GetSessoesCountPorUsuarioAsync(int usuarioId);
    }
}
