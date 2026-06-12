using NutriFlow.Models;
using System.Threading.Tasks;

namespace NutriFlow.Repositories
{
    public interface IUsuarioRepository : IRepository<Usuario>
    {
        Task<Usuario?> GetByEmailAsync(string email);
        Task<bool> ExisteUsuarioAsync(string nome, string email);
    }
}
