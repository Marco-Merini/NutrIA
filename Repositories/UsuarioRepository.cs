using Microsoft.EntityFrameworkCore;
using NutriFlow.Data;
using NutriFlow.Models;
using System.Threading.Tasks;

namespace NutriFlow.Repositories
{
    public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
    {
        public UsuarioRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email && u.Ativo == "S");
        }

        public async Task<bool> ExisteUsuarioAsync(string nome, string email)
        {
            return await _dbSet.AnyAsync(u => u.Nome == nome || u.Email == email);
        }
    }
}
