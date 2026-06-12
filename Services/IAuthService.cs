using NutriFlow.Models;
using System.Threading.Tasks;

namespace NutriFlow.Services
{
    public interface IAuthService
    {
        Usuario? CurrentUser { get; }
        Task InitializeAsync();
        string GenerateJwtToken(Usuario user);
        Task<(bool Success, string? Token)> LoginAsync(string email, string senha);
        Task<bool> RegisterAsync(Usuario newUser);
        Task<bool> UpdateUserAsync(Usuario updatedUser);
    }
}
