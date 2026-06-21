using Xunit;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using NutriFlow.Models;
using NutriFlow.Services;
using NutriFlow.Repositories;

namespace NutriFlow.Tests
{
    public class IndexacaoBackgroundServiceTests
    {
        [Fact]
        public async Task IndexacaoBackgroundService_ProcessesQueueSuccessfully()
        {
            // Arrange
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            var pacienteRepoMock = new Mock<IPacienteRepository>();
            var embeddingRepoMock = new Mock<IEmbeddingChunkRepository>();
            var loggerMock = new Mock<ILogger<IndexacaoBackgroundService>>();

            scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
            scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
            
            // Register repo mocks
            serviceProviderMock.Setup(p => p.GetService(typeof(IPacienteRepository))).Returns(pacienteRepoMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(IEmbeddingChunkRepository))).Returns(embeddingRepoMock.Object);

            // Mock OpenAI clients for AIService
            var chatClientMock = new Mock<ChatClient>();
            var embeddingClientMock = new Mock<EmbeddingClient>();

            var floats = new float[1536];
            floats[0] = 0.77f;
            var bytes = new byte[1536 * 4];
            Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
            var base64 = Convert.ToBase64String(bytes);
            var collectionJson = @"{ ""data"": [ { ""index"": 0, ""embedding"": """ + base64 + @""" } ] }";
            var mockCollectionResult = ModelReaderWriter.Read<OpenAIEmbeddingCollection>(BinaryData.FromString(collectionJson));

            embeddingClientMock.Setup(e => e.GenerateEmbeddingsAsync(
                It.IsAny<IEnumerable<string>>(), 
                It.IsAny<EmbeddingGenerationOptions>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientResult.FromValue(mockCollectionResult, new Mock<PipelineResponse>().Object));

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"RAG:ChatModel", "gpt-4o-mini"},
                {"RAG:EmbeddingModel", "text-embedding-3-small"}
            }).Build();

            var aiService = new AIService(scopeFactoryMock.Object, config, chatClientMock.Object, embeddingClientMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(AIService))).Returns(aiService);

            // Mock Patient retrieval
            var paciente = new Paciente
            {
                Id = 10,
                Nome = "Paciente Teste",
                Sexo = "Feminino",
                PesoAtual = 60.0M,
                Altura = 1.60M,
                DataNascimento = DateTime.Now.AddYears(-22)
            };
            pacienteRepoMock.Setup(r => r.GetPacienteCompletoSemUsuarioAsync(10))
                .ReturnsAsync(paciente);

            var service = new IndexacaoBackgroundService(scopeFactoryMock.Object, loggerMock.Object);

            var cts = new CancellationTokenSource();

            // Act
            await service.StartAsync(cts.Token);
            service.EnquileirarReindexacao(10, "cadastro");

            // Give it 100ms to process the queue in background
            await Task.Delay(100);

            // Assert
            pacienteRepoMock.Verify(r => r.GetPacienteCompletoSemUsuarioAsync(10), Times.Once);
            embeddingClientMock.Verify(e => e.GenerateEmbeddingsAsync(
                It.IsAny<IEnumerable<string>>(), 
                It.IsAny<EmbeddingGenerationOptions>(), 
                It.IsAny<CancellationToken>()), Times.Once);

            // Cleanup
            await service.StopAsync(CancellationToken.None);
            cts.Cancel();
        }
    }
}
