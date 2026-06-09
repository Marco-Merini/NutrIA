using NutriFlow.Models;
using NutriFlow.Repositories;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NutriFlow.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IConfiguration _configuration;
        private Usuario? _currentUser;
        private bool _isInitialized;

        public AuthService(IUsuarioRepository usuarioRepository, AuthenticationStateProvider authStateProvider, IConfiguration configuration)
        {
            _usuarioRepository = usuarioRepository;
            _authStateProvider = authStateProvider;
            _configuration = configuration;
        }

        public Usuario? CurrentUser => _currentUser;

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var userClaim = authState.User.FindFirst("UsuarioId")?.Value;

                if (int.TryParse(userClaim, out int userId) && userId > 0)
                {
                    _currentUser = await _usuarioRepository.GetByIdAsync(userId);
                }
                else
                {
                    _currentUser = null;
                }
            }
            catch
            {
                // Fallback gracefully on initialization error
            }
            finally
            {
                _isInitialized = true;
            }
        }

        public string GenerateJwtToken(Usuario user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? _configuration.GetValue<string>("Jwt:Key")
                ?? throw new InvalidOperationException("JWT secret key is not configured. Set the JWT_SECRET_KEY environment variable or use 'dotnet user-secrets'.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Nome ?? ""),
                new Claim("UsuarioId", user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("Usu_Categoria", user.Tipo ?? "Nutricionista")
            };

            var expireHours = int.Parse(jwtSettings["ExpireHours"] ?? "24");

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expireHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<(bool Success, string? Token)> LoginAsync(string email, string senha)
        {
            var user = await _usuarioRepository.GetByEmailAsync(email);

            if (user == null || string.IsNullOrEmpty(user.Senha))
            {
                return (false, null);
            }

            if (!BCrypt.Net.BCrypt.Verify(senha, user.Senha))
            {
                return (false, null);
            }

            var token = GenerateJwtToken(user);
            return (true, token);
        }

        public async Task<bool> RegisterAsync(Usuario newUser)
        {
            try
            {
                var exists = await _usuarioRepository.ExisteUsuarioAsync(newUser.Nome ?? "", newUser.Email ?? "");
                if (exists)
                {
                    return false;
                }

                newUser.Senha = BCrypt.Net.BCrypt.HashPassword(newUser.Senha);

                await _usuarioRepository.AddAsync(newUser);
                await _usuarioRepository.SaveChangesAsync();

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
                var user = await _usuarioRepository.GetByIdAsync(updatedUser.Id);
                if (user == null) return false;

                user.Nome = updatedUser.Nome;
                user.Email = updatedUser.Email;
                if (!string.IsNullOrEmpty(updatedUser.Senha) && updatedUser.Senha != user.Senha)
                {
                    user.Senha = BCrypt.Net.BCrypt.HashPassword(updatedUser.Senha);
                }

                user.DataAtualizacao = DateTime.Now;

                _usuarioRepository.Update(user);
                await _usuarioRepository.SaveChangesAsync();

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
    }
}
