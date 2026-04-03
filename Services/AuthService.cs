using NutriFlow.Models;
using NutriFlow.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Cryptography;
using System.Text;

namespace NutriFlow.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ProtectedLocalStorage _localStorage;
        private Usuario? _currentUser;
        private bool _isAuthenticated;
        private bool _isInitialized;

        public event Action? AuthStateChanged;

        public AuthService(ApplicationDbContext dbContext, ProtectedLocalStorage localStorage)
        {
            _dbContext = dbContext;
            _localStorage = localStorage;
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            private set
            {
                if (_isAuthenticated != value)
                {
                    _isAuthenticated = value;
                    AuthStateChanged?.Invoke();
                }
            }
        }

        public Usuario? CurrentUser => _currentUser;

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                var result = await _localStorage.GetAsync<int>("userId");
                if (result.Success && result.Value > 0)
                {
                    _currentUser = await _dbContext.Usuarios.FindAsync(result.Value);
                    if (_currentUser != null)
                    {
                        IsAuthenticated = true;
                    }
                }
            }
            catch
            {
                
            }
            finally
            {
                _isInitialized = true;
            }
        }

        public string GenerateHash(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public async Task<bool> RegisterAsync(Usuario newUser)
        {
            try
            {
                var exists = await _dbContext.Usuarios.AnyAsync(u => u.Nome == newUser.Nome || u.Email == newUser.Email);
                if (exists)
                {
                    return false;
                }

                newUser.Senha = GenerateHash(newUser.Senha);
                
                await _dbContext.Usuarios.AddAsync(newUser);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar conta: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(Usuario updatedUser)
        {
            try
            {
                var user = await _dbContext.Usuarios.FindAsync(updatedUser.Id);
                if (user == null) return false;

                user.Nome = updatedUser.Nome;
                user.Email = updatedUser.Email;
                if (!string.IsNullOrEmpty(updatedUser.Senha) && updatedUser.Senha != user.Senha)
                {
                    user.Senha = GenerateHash(updatedUser.Senha);
                }

                user.DataAtualizacao = DateTime.Now;
                
                _dbContext.Usuarios.Update(user);
                await _dbContext.SaveChangesAsync();

                if (_currentUser?.Id == user.Id)
                {
                    _currentUser = user;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao editar perfil: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> LoginAsync(string nome, string senha)
        {
            try
            {
                string hashedSenha = GenerateHash(senha);
                var user = await _dbContext.Usuarios.FirstOrDefaultAsync(u => u.Nome == nome && u.Senha == hashedSenha && u.Ativo == true);

                if (user != null)
                {
                    _currentUser = user;
                    IsAuthenticated = true;
                    
                    try 
                    {
                        await _localStorage.SetAsync("userId", user.Id);
                    } 
                    catch { 
                    }
                    
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no login: {ex.Message}");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            _currentUser = null;
            IsAuthenticated = false;
            _isInitialized = false;
            
            try
            {
                await _localStorage.DeleteAsync("userId");
            }
            catch { }
        }
    }
}
