using Xunit;
using Moq;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using NutriFlow.Models;
using NutriFlow.Services;
using NutriFlow.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace NutriFlow.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<AuthenticationStateProvider> _authStateProviderMock;
        private readonly IConfiguration _config;
        private readonly List<Usuario> _usersList;

        public AuthServiceTests()
        {
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _authStateProviderMock = new Mock<AuthenticationStateProvider>();
            _usersList = new List<Usuario>();

            var myConfiguration = new Dictionary<string, string?>
            {
                {"Jwt:Issuer", "NutriAI"},
                {"Jwt:Audience", "NutriAI_Users"},
                {"Jwt:ExpireHours", "24"},
                {"Jwt:Key", "your-secret-key-here-minimum-32-characters-long!"}
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();

            _usuarioRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((string email) => _usersList.FirstOrDefault(u => u.Email == email));

            _usuarioRepoMock.Setup(r => r.AddAsync(It.IsAny<Usuario>()))
                .Callback<Usuario>(u => _usersList.Add(u))
                .Returns(Task.CompletedTask);

            _usuarioRepoMock.Setup(r => r.ExisteUsuarioAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string nome, string email) => _usersList.Any(u => u.Nome == nome || u.Email == email));
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsTrueAndToken()
        {
            // Arrange
            var plainPassword = "SecurePassword123";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

            var user = new Usuario
            {
                Id = 1,
                Nome = "Nutricionista Teste",
                Email = "teste@nutriflow.com",
                Senha = hashedPassword,
                Ativo = "S",
                Tipo = "Nutricionista"
            };

            _usersList.Add(user);

            var service = new AuthService(_usuarioRepoMock.Object, _authStateProviderMock.Object, _config);

            // Act
            var (success, token) = await service.LoginAsync("teste@nutriflow.com", plainPassword);

            // Assert
            Assert.True(success);
            Assert.NotNull(token);

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            Assert.Equal("NutriAI", jwtToken.Issuer);
            Assert.Contains(jwtToken.Claims, c => c.Type == "UsuarioId" && c.Value == "1");
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsFalse()
        {
            // Arrange
            var plainPassword = "SecurePassword123";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

            var user = new Usuario
            {
                Id = 2,
                Nome = "Nutricionista Teste 2",
                Email = "teste2@nutriflow.com",
                Senha = hashedPassword,
                Ativo = "S"
            };

            _usersList.Add(user);

            var service = new AuthService(_usuarioRepoMock.Object, _authStateProviderMock.Object, _config);

            // Act
            var (success, token) = await service.LoginAsync("teste2@nutriflow.com", "WrongPassword");

            // Assert
            Assert.False(success);
            Assert.Null(token);
        }

        [Fact]
        public async Task RegisterAsync_NewUser_EncryptsPasswordAndSaves()
        {
            // Arrange
            var service = new AuthService(_usuarioRepoMock.Object, _authStateProviderMock.Object, _config);
            var newUser = new Usuario
            {
                Nome = "Novo Nutri",
                Email = "novo@nutri.com",
                Senha = "MyPlainPassword",
                Ativo = "S"
            };

            // Act
            var success = await service.RegisterAsync(newUser);

            // Assert
            Assert.True(success);
            var savedUser = _usersList.FirstOrDefault(u => u.Email == "novo@nutri.com");
            Assert.NotNull(savedUser);
            Assert.NotEqual("MyPlainPassword", savedUser.Senha);
            Assert.True(BCrypt.Net.BCrypt.Verify("MyPlainPassword", savedUser.Senha));
        }

        [Fact]
        public async Task InitializeAsync_WhenUserIsAuthenticated_LoadsCurrentUser()
        {
            // Arrange
            var user = new Usuario { Id = 10, Nome = "Nutri Autenticado", Email = "auth@nutri.com" };
            _usersList.Add(user);
            _usuarioRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);

            var claims = new List<Claim> { new Claim("UsuarioId", "10") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var authState = new AuthenticationState(principal);

            _authStateProviderMock.Setup(p => p.GetAuthenticationStateAsync())
                .ReturnsAsync(authState);

            var service = new AuthService(_usuarioRepoMock.Object, _authStateProviderMock.Object, _config);

            // Act
            await service.InitializeAsync();

            // Assert
            Assert.NotNull(service.CurrentUser);
            Assert.Equal(10, service.CurrentUser.Id);
            _usuarioRepoMock.Verify(r => r.GetByIdAsync(10), Times.Once);

            // Calling initialize again should return immediately (already initialized)
            await service.InitializeAsync();
            _usuarioRepoMock.Verify(r => r.GetByIdAsync(10), Times.Once); // Still once
        }

        [Fact]
        public async Task InitializeAsync_WhenUserNotAuthenticated_SetsCurrentUserToNull()
        {
            // Arrange
            var principal = new ClaimsPrincipal(new ClaimsIdentity()); // Unauthenticated
            var authState = new AuthenticationState(principal);

            _authStateProviderMock.Setup(p => p.GetAuthenticationStateAsync())
                .ReturnsAsync(authState);

            var service = new AuthService(_usuarioRepoMock.Object, _authStateProviderMock.Object, _config);

            // Act
            await service.InitializeAsync();

            // Assert
            Assert.Null(service.CurrentUser);
        }

        [Fact]
        public async Task InitializeAsync_WhenGetAuthenticationStateThrows_HandlesGracefully()
        {
            // Arrange
            _authStateProviderMock.Setup(p => p.GetAuthenticationStateAsync())
                .ThrowsAsync(new Exception("Simulated provider error"));

            var service = new AuthService(_usuarioRepoMock.Object, _authStateProviderMock.Object, _config);

            // Act & Assert (Should not throw exception)
            var exception = await Record.ExceptionAsync(() => service.InitializeAsync());
            Assert.Null(exception);
            Assert.Null(service.CurrentUser);
        }

        [Fact]
        public async Task UpdateUserAsync_WhenUserExists_UpdatesSuccessfully()
        {
            // Arrange
            var user = new Usuario { Id = 5, Nome = "Nome Antigo", Email = "antigo@nutri.com", Senha = "OldHash" };
            _usersList.Add(user);
            _usuarioRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);

            var service = new AuthService(_usuarioRepoMock.Object, _authStateProviderMock.Object, _config);
            var updated = new Usuario { Id = 5, Nome = "Nome Novo", Email = "novo@nutri.com", Senha = "NewPlainPassword" };

            // Act
            var result = await service.UpdateUserAsync(updated);

            // Assert
            Assert.True(result);
            Assert.Equal("Nome Novo", user.Nome);
            Assert.Equal("novo@nutri.com", user.Email);
            Assert.NotEqual("NewPlainPassword", user.Senha);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewPlainPassword", user.Senha));
            _usuarioRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_WhenUserDoesNotExist_ReturnsFalse()
        {
            // Arrange
            _usuarioRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Usuario?)null);
            var service = new AuthService(_usuarioRepoMock.Object, _authStateProviderMock.Object, _config);

            // Act
            var result = await service.UpdateUserAsync(new Usuario { Id = 99 });

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserAsync_WhenRepoThrowsException_ReturnsFalse()
        {
            // Arrange
            _usuarioRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("DB connection lost"));
            var service = new AuthService(_usuarioRepoMock.Object, _authStateProviderMock.Object, _config);

            // Act
            var result = await service.UpdateUserAsync(new Usuario { Id = 1 });

            // Assert
            Assert.False(result);
        }
    }
}
