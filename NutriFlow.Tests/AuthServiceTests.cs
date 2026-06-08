using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using NutriFlow.Data;
using NutriFlow.Models;
using NutriFlow.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;

namespace NutriFlow.Tests
{
    public class AuthServiceTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
        private readonly Mock<AuthenticationStateProvider> _authStateProviderMock;
        private readonly IConfiguration _config;

        public AuthServiceTests()
        {
            _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _authStateProviderMock = new Mock<AuthenticationStateProvider>();

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
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsTrueAndToken()
        {
            // Arrange
            using var dbContext = new ApplicationDbContext(_dbContextOptions);
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

            await dbContext.Usuarios.AddAsync(user);
            await dbContext.SaveChangesAsync();

            var service = new AuthService(dbContext, _authStateProviderMock.Object, _config);

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
            using var dbContext = new ApplicationDbContext(_dbContextOptions);
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

            await dbContext.Usuarios.AddAsync(user);
            await dbContext.SaveChangesAsync();

            var service = new AuthService(dbContext, _authStateProviderMock.Object, _config);

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
            using var dbContext = new ApplicationDbContext(_dbContextOptions);
            var service = new AuthService(dbContext, _authStateProviderMock.Object, _config);
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
            var savedUser = await dbContext.Usuarios.FirstOrDefaultAsync(u => u.Email == "novo@nutri.com");
            Assert.NotNull(savedUser);
            Assert.NotEqual("MyPlainPassword", savedUser.Senha);
            Assert.True(BCrypt.Net.BCrypt.Verify("MyPlainPassword", savedUser.Senha));
        }
    }
}
