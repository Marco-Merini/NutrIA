using NutriFlow.Models;
using System.Security.Cryptography;
using System.Text;

namespace NutriFlow.Services
{
    public class AuthService
    {
        private Usuario? _currentUser;
        private bool _isAuthenticated;
        
        // Mock storage para dev
        private List<Usuario> _usuariosMock = new();

        public event Action? AuthStateChanged;

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

        public string GenerateHash(string input)
        {
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
            await Task.Delay(500); // simulate network
            
            // Hash the password deterministic
            newUser.Senha = GenerateHash(newUser.Senha);
            newUser.Id = _usuariosMock.Count + 1;
            
            _usuariosMock.Add(newUser);
            return true;
        }

        public async Task<bool> LoginAsync(string nome, string senha)
        {
            try
            {
                // Simular delay de rede
                await Task.Delay(500);

                string hashedSenha = GenerateHash(senha);

                // Find user
                var user = _usuariosMock.FirstOrDefault(u => u.Nome == nome && u.Senha == hashedSenha);

                if (user != null)
                {
                    _currentUser = user;
                    IsAuthenticated = true;
                    return true;
                }
                
                // Fallback for initial login before registration
                if (_usuariosMock.Count == 0 && nome == "Nutricionista" && senha == "123")
                {
                    _currentUser = new Usuario
                    {
                        Id = 1,
                        Nome = "Nutricionista",
                        Email = "nutri@email.com",
                        Tipo = "Nutricionista",
                        Ativo = true,
                        DataCriacao = DateTime.Now
                    };
                    IsAuthenticated = true;
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

        public void Logout()
        {
            _currentUser = null;
            IsAuthenticated = false;
        }
    }
}
