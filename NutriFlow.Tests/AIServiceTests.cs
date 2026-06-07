using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NutriFlow.Models;
using NutriFlow.Services;
using System.Collections.Generic;
using System;

namespace NutriFlow.Tests
{
    public class AIServiceTests
    {
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly IConfiguration _config;

        public AIServiceTests()
        {
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();

            var myConfiguration = new Dictionary<string, string?>
            {
                {"RAG:ChatModel", "gpt-4o-mini"},
                {"RAG:EmbeddingModel", "text-embedding-3-small"},
                {"RAG:TopK", "8"},
                {"RAG:ScoreThreshold", "0.25"}
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();
        }

        [Fact]
        public void GenerateChunksFromPaciente_AppliesLGPDPseudonymization()
        {
            // Arrange
            var dummyApiKey = "sk-proj-dummyApiKey123456789012345678901234567890";
            var service = new AIService(dummyApiKey, _scopeFactoryMock.Object, _config);

            // Adicionamos AddDays(-5) para garantir que o aniversário já passou no ano atual
            var birthDate = DateTime.Now.AddYears(-30).AddDays(-5);

            var paciente = new Paciente
            {
                Id = 42,
                Nome = "Maria Silva Santos",
                Email = "maria.silva@exemplo.com",
                Telefone = "(11) 98765-4321",
                DataNascimento = birthDate,
                Sexo = "Feminino",
                PesoAtual = 68.5M,
                Altura = 1.65M,
                Objetivo = "Hipertrofia",
                NivelAtividade = "Moderado",
                CondicoesSaude = "Hipertensão leve",
                PreferenciasAlimentares = "Vegetariana",
                Medicamentos = "Nenhum"
            };

            // Act
            var chunks = service.GenerateChunksFromPaciente(paciente);

            // Assert
            Assert.NotEmpty(chunks);

            // Verificar que NENHUM dos chunks gerados contém os dados identificáveis (Nome, Email, Telefone)
            foreach (var chunk in chunks)
            {
                Assert.DoesNotContain("Maria", chunk.Text);
                Assert.DoesNotContain("Silva", chunk.Text);
                Assert.DoesNotContain("Santos", chunk.Text);
                Assert.DoesNotContain("maria.silva", chunk.Text);
                Assert.DoesNotContain("98765", chunk.Text);

                // Deve conter dados de saúde e parâmetros clínicos
                if (chunk.Tipo == "perfil")
                {
                    Assert.Contains("30 anos", chunk.Text);
                    Assert.Contains("Feminino", chunk.Text);
                    Assert.True(chunk.Text.Contains("68.5kg") || chunk.Text.Contains("68,5kg"), "Deveria conter o peso atual do paciente.");
                    Assert.True(chunk.Text.Contains("1.65m") || chunk.Text.Contains("1,65m"), "Deveria conter a altura do paciente.");
                    Assert.Contains("Hipertrofia", chunk.Text);
                }
                else if (chunk.Tipo == "saude")
                {
                    Assert.Contains("Hipertensão leve", chunk.Text);
                }
                else if (chunk.Tipo == "preferencias")
                {
                    Assert.Contains("Vegetariana", chunk.Text);
                }
            }
        }
    }
}
