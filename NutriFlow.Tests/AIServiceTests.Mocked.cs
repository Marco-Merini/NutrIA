using Xunit;
using Moq;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NutriFlow.Models;
using NutriFlow.Services;
using NutriFlow.Repositories;
using NutriFlow.Models.Rag;

namespace NutriFlow.Tests
{
    public class AIServiceTestsMocked
    {
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<IServiceScope> _scopeMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IEmbeddingChunkRepository> _embeddingRepoMock;
        private readonly IConfiguration _config;

        public AIServiceTestsMocked()
        {
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _scopeMock = new Mock<IServiceScope>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _embeddingRepoMock = new Mock<IEmbeddingChunkRepository>();

            _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
            _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
            _serviceProviderMock.Setup(p => p.GetService(typeof(IEmbeddingChunkRepository))).Returns(_embeddingRepoMock.Object);

            var myConfiguration = new Dictionary<string, string?>
            {
                {"RAG:ChatModel", "gpt-4o-mini"},
                {"RAG:EmbeddingModel", "text-embedding-3-small"},
                {"RAG:TopK", "2"},
                {"RAG:ScoreThreshold", "0.1"}
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();
        }

        private static string GetEmbeddingJson(float[] floats)
        {
            var bytes = new byte[floats.Length * 4];
            Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
            var base64 = Convert.ToBase64String(bytes);
            return @"{ ""embedding"": """ + base64 + @""" }";
        }

        private static string GetEmbeddingCollectionJson(List<float[]> embeddings)
        {
            var dataList = new List<string>();
            for (int i = 0; i < embeddings.Count; i++)
            {
                var bytes = new byte[embeddings[i].Length * 4];
                Buffer.BlockCopy(embeddings[i], 0, bytes, 0, bytes.Length);
                var base64 = Convert.ToBase64String(bytes);
                dataList.Add($@"{{ ""index"": {i}, ""embedding"": ""{base64}"" }}");
            }
            return $@"{{ ""data"": [ {string.Join(",", dataList)} ] }}";
        }

        [Fact]
        public async Task LoadIndexFromDbAsync_WhenNoStoredChunks_ReturnsFalse()
        {
            // Arrange
            var chatClientMock = new Mock<ChatClient>();
            var embeddingClientMock = new Mock<EmbeddingClient>();

            _embeddingRepoMock.Setup(r => r.GetChunksByPacienteIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<EmbeddingChunk>());

            var service = new AIService(_scopeFactoryMock.Object, _config, chatClientMock.Object, embeddingClientMock.Object);

            // Act
            var result = await service.LoadIndexFromDbAsync(1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task LoadIndexFromDbAsync_WhenChunksExist_LoadsThemAndReturnsTrue()
        {
            // Arrange
            var chatClientMock = new Mock<ChatClient>();
            var embeddingClientMock = new Mock<EmbeddingClient>();

            var floats = new float[1536];
            floats[0] = 0.5f;
            var json = JsonSerializer.Serialize(floats);

            var storedChunks = new List<EmbeddingChunk>
            {
                new() { PatientId = 1, SourceTable = "Pacientes", SourceId = 1, ChunkType = "perfil", ChunkText = "Texto chunk 1", EmbeddingJson = json }
            };

            _embeddingRepoMock.Setup(r => r.GetChunksByPacienteIdAsync(1))
                .ReturnsAsync(storedChunks);

            var service = new AIService(_scopeFactoryMock.Object, _config, chatClientMock.Object, embeddingClientMock.Object);

            // Act
            var result = await service.LoadIndexFromDbAsync(1);

            // Assert
            Assert.True(result);
            Assert.Equal(1, service.CurrentIndexedPacienteId);
            Assert.Single(service.IndexedChunks);
            Assert.Equal("Texto chunk 1", service.IndexedChunks[0].Text);
            Assert.Equal(0.5f, service.IndexedChunks[0].Embedding[0]);
        }

        [Fact]
        public async Task IndexPacienteAsync_GeneratesEmbeddingsAndSavesToDb()
        {
            // Arrange
            var chatClientMock = new Mock<ChatClient>();
            var embeddingClientMock = new Mock<EmbeddingClient>();

            // Setup mock for GenerateEmbeddingsAsync using Base64 helper
            var floats = new float[1536];
            floats[0] = 0.9f;
            var collectionJson = GetEmbeddingCollectionJson(new List<float[]> { floats });
            var mockCollectionResult = ModelReaderWriter.Read<OpenAIEmbeddingCollection>(BinaryData.FromString(collectionJson));
            var clientResult = ClientResult.FromValue(mockCollectionResult, new Mock<PipelineResponse>().Object);

            embeddingClientMock.Setup(e => e.GenerateEmbeddingsAsync(
                It.IsAny<IEnumerable<string>>(), 
                It.IsAny<EmbeddingGenerationOptions>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(clientResult);

            var service = new AIService(_scopeFactoryMock.Object, _config, chatClientMock.Object, embeddingClientMock.Object);

            var paciente = new Paciente
            {
                Id = 1,
                Nome = "Lucas",
                Sexo = "Masculino",
                PesoAtual = 80.0M,
                Altura = 1.80M,
                DataNascimento = DateTime.Now.AddYears(-25)
            };

            // Act
            await service.IndexPacienteAsync(paciente);

            // Assert
            Assert.True(service.IsIndexed(1));
            var summary = service.GetIndexSummary(1);
            Assert.True(summary.ContainsKey("perfil"));
            Assert.Equal(0.9f, service.IndexedChunks[0].Embedding[0]);

            // Give background thread a tiny moment to invoke repository call
            await Task.Delay(50);
            _embeddingRepoMock.Verify(r => r.DeleteChunksByPacienteIdAsync(1), Times.Once);
            _embeddingRepoMock.Verify(r => r.AddAsync(It.IsAny<EmbeddingChunk>()), Times.AtLeastOnce);
            _embeddingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AskQuestionRagAsync_WhenNotIndexed_ReturnsWarning()
        {
            // Arrange
            var chatClientMock = new Mock<ChatClient>();
            var embeddingClientMock = new Mock<EmbeddingClient>();
            var service = new AIService(_scopeFactoryMock.Object, _config, chatClientMock.Object, embeddingClientMock.Object);

            var paciente = new Paciente { Id = 2 };

            // Act
            var result = await service.AskQuestionRagAsync("Qual é a dieta?", paciente);

            // Assert
            Assert.Contains("ainda não foi indexado", result.Answer.ToLower());
            Assert.False(result.UsouDadosIndexados);
        }

        [Fact]
        public async Task AskQuestionRagAsync_WhenIndexed_CallsEmbeddingAndChatClients()
        {
            // Arrange
            var chatClientMock = new Mock<ChatClient>();
            var embeddingClientMock = new Mock<EmbeddingClient>();

            // Mock embedding for query using Base64 helper
            var queryVector = new float[1536];
            queryVector[0] = 0.1f;
            var embeddingJson = GetEmbeddingJson(queryVector);
            var mockEmbeddingResult = ModelReaderWriter.Read<OpenAIEmbedding>(BinaryData.FromString(embeddingJson));
            var clientResult = ClientResult.FromValue(mockEmbeddingResult, new Mock<PipelineResponse>().Object);

            embeddingClientMock.Setup(e => e.GenerateEmbeddingAsync(
                It.IsAny<string>(), 
                It.IsAny<EmbeddingGenerationOptions>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(clientResult);

            // Mock chat completion
            var chatJson = @"{
                ""choices"": [
                    {
                        ""message"": {
                            ""role"": ""assistant"",
                            ""content"": ""Resposta RAG Mockada""
                        }
                    }
                ]
            }";
            var mockChatCompletion = ModelReaderWriter.Read<ChatCompletion>(BinaryData.FromString(chatJson));
            var chatResult = ClientResult.FromValue(mockChatCompletion, new Mock<PipelineResponse>().Object);

            chatClientMock.Setup(c => c.CompleteChatAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), 
                It.IsAny<ChatCompletionOptions>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(chatResult);

            // Setup stored chunks in DB to load first
            var storedFloats = new float[1536];
            storedFloats[0] = 0.8f;
            var storedChunks = new List<EmbeddingChunk>
            {
                new() { PatientId = 1, SourceTable = "Pacientes", SourceId = 1, ChunkType = "perfil", ChunkText = "Perfil do Paciente", EmbeddingJson = JsonSerializer.Serialize(storedFloats) }
            };
            _embeddingRepoMock.Setup(r => r.GetChunksByPacienteIdAsync(1))
                .ReturnsAsync(storedChunks);

            var service = new AIService(_scopeFactoryMock.Object, _config, chatClientMock.Object, embeddingClientMock.Object);
            await service.LoadIndexFromDbAsync(1);

            var paciente = new Paciente { Id = 1, Nome = "Lucas" };

            // Act
            var response = await service.AskQuestionRagAsync("Quem é Lucas?", paciente);

            // Assert
            Assert.Equal("Resposta RAG Mockada", response.Answer);
            Assert.True(response.UsouDadosIndexados);
            Assert.Single(response.Sources);
            Assert.Equal("Pacientes", response.Sources[0].Tabela);
        }

        [Fact]
        public async Task AskQuestionAsync_ReturnsResponse()
        {
            // Arrange
            var chatClientMock = new Mock<ChatClient>();
            var embeddingClientMock = new Mock<EmbeddingClient>();

            var queryVector = new float[1536];
            var embeddingJson = GetEmbeddingJson(queryVector);
            var mockEmbeddingResult = ModelReaderWriter.Read<OpenAIEmbedding>(BinaryData.FromString(embeddingJson));
            embeddingClientMock.Setup(e => e.GenerateEmbeddingAsync(
                It.IsAny<string>(), 
                It.IsAny<EmbeddingGenerationOptions>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientResult.FromValue(mockEmbeddingResult, new Mock<PipelineResponse>().Object));

            var chatJson = @"{
                ""choices"": [
                    {
                        ""message"": {
                            ""role"": ""assistant"",
                            ""content"": ""Resposta Simples Mockada""
                        }
                    }
                ]
            }";
            var mockChatCompletion = ModelReaderWriter.Read<ChatCompletion>(BinaryData.FromString(chatJson));
            chatClientMock.Setup(c => c.CompleteChatAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), 
                It.IsAny<ChatCompletionOptions>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientResult.FromValue(mockChatCompletion, new Mock<PipelineResponse>().Object));

            // Load index
            var storedChunks = new List<EmbeddingChunk>
            {
                new() { PatientId = 1, SourceTable = "Pacientes", SourceId = 1, ChunkType = "perfil", ChunkText = "Texto", EmbeddingJson = JsonSerializer.Serialize(new float[1536]) }
            };
            _embeddingRepoMock.Setup(r => r.GetChunksByPacienteIdAsync(1)).ReturnsAsync(storedChunks);

            var service = new AIService(_scopeFactoryMock.Object, _config, chatClientMock.Object, embeddingClientMock.Object);
            await service.LoadIndexFromDbAsync(1);

            // Act
            var answer = await service.AskQuestionAsync("Pergunta teste?");

            // Assert
            Assert.Equal("Resposta Simples Mockada", answer);
        }

        [Fact]
        public async Task GenerateDietPlanAsync_ParsesJsonStructuredResponse()
        {
            // Arrange
            var chatClientMock = new Mock<ChatClient>();
            var embeddingClientMock = new Mock<EmbeddingClient>();

            var planJson = @"{
                ""Titulo"": ""Plano de Dieta"",
                ""Objetivo"": ""Hipertrofia"",
                ""CaloriasDiarias"": 2500,
                ""ProteinasG"": 150,
                ""CarboidratosG"": 300,
                ""GordurasG"": 70,
                ""Orientacoes"": ""Beber agua"",
                ""ObservacoesNutricionista"": ""Nenhuma"",
                ""Refeicoes"": [
                    { ""Nome"": ""Cafe"", ""HorarioTexto"": ""08:00"", ""Alimentos"": ""Ovos"", ""Calorias"": 400 }
                ]
            }";

            var chatJson = $@"{{
                ""choices"": [
                    {{
                        ""message"": {{
                            ""role"": ""assistant"",
                            ""content"": {JsonSerializer.Serialize(planJson)}
                        }}
                    }}
                ]
            }}";

            var mockChatCompletion = ModelReaderWriter.Read<ChatCompletion>(BinaryData.FromString(chatJson));
            chatClientMock.Setup(c => c.CompleteChatAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), 
                It.IsAny<ChatCompletionOptions>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientResult.FromValue(mockChatCompletion, new Mock<PipelineResponse>().Object));

            var service = new AIService(_scopeFactoryMock.Object, _config, chatClientMock.Object, embeddingClientMock.Object);
            var paciente = new Paciente { Id = 1, Nome = "Lucas" };

            // Act
            var plan = await service.GenerateDietPlanAsync(paciente);

            // Assert
            Assert.NotNull(plan);
            Assert.Equal("Plano de Dieta", plan.Titulo);
            Assert.Equal(2500M, plan.CaloriasDiarias);
            Assert.Single(plan.Refeicoes);
            Assert.Equal("Cafe", plan.Refeicoes[0].Nome);
        }
    }
}
