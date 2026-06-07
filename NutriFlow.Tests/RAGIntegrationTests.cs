using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NutriFlow.Data;
using NutriFlow.Models;
using NutriFlow.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace NutriFlow.Tests
{
    public class RAGIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public RAGIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestRAGAcrossAllTables()
        {
            // 1. Carregar API Key do arquivo .env
            string envPath = Path.Combine(AppContext.BaseDirectory, "../../../../.env");
            if (!File.Exists(envPath))
            {
                _output.WriteLine("Arquivo .env não encontrado no caminho: " + Path.GetFullPath(envPath));
                return;
            }

            var envLines = await File.ReadAllLinesAsync(envPath);
            var apiKeyLine = envLines.FirstOrDefault(l => l.StartsWith("OPENAI_API_KEY="));
            if (apiKeyLine == null)
            {
                _output.WriteLine("OPENAI_API_KEY não configurada no arquivo .env");
                return;
            }

            string apiKey = apiKeyLine.Split('=')[1].Trim();
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("YOUR_"))
            {
                _output.WriteLine("OPENAI_API_KEY vazia ou inválida.");
                return;
            }

            // 2. Setup Configuration
            var myConfiguration = new Dictionary<string, string?>
            {
                {"RAG:ChatModel", "gpt-4o-mini"},
                {"RAG:EmbeddingModel", "text-embedding-3-small"},
                {"RAG:TopK", "8"},
                {"RAG:ScoreThreshold", "0.25"}
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();

            // 3. Setup In-Memory DbContext
            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            using var db = new ApplicationDbContext(dbOptions);

            // 4. Inserir dados do paciente teste em todas as tabelas
            var paciente = new Paciente
            {
                Id = 1,
                Nome = "Lucas Merini",
                DataNascimento = DateTime.Now.AddYears(-27),
                Sexo = "Masculino",
                PesoAtual = 82.5M,
                Altura = 1.78M,
                Objetivo = "Hipertrofia",
                NivelAtividade = "Moderado",
                PreferenciasAlimentares = "Evita frituras e refrigerante",
                CondicoesSaude = "Leve intolerância à lactose",
                Status = "ativo",
                DataCadastro = DateTime.Now.AddDays(-30),
                DataAtualizacao = DateTime.Now
            };

            var sessao = new Sessao
            {
                Id = 10,
                PacienteId = 1,
                Tipo = "Retorno",
                DataSessao = DateTime.Now.AddDays(-15),
                PesoSessao = 83.0M,
                Anotacoes = "Paciente focado no objetivo de ganho de massa, porém relatou leve desconforto abdominal e pediu ajustes.",
                ProximaConsulta = DateTime.Now.AddDays(15),
                DataAtualizacao = DateTime.Now
            };

            var progresso = new Progresso
            {
                Id = 20,
                PacienteId = 1,
                DataRegistro = DateTime.Now.AddDays(-7),
                Peso = 82.5M,
                CinturaCm = 84,
                QuadrilCm = 98,
                PercentualGordura = 14,
                AderenciaPlano = "Excelente",
                Humor = "Bom",
                Energia = "Alta",
                Feedback = "Sentindo-se mais forte nos treinos e adaptado.",
                DataAtualizacao = DateTime.Now
            };

            var plano = new PlanoDieta
            {
                Id = 30,
                PacienteId = 1,
                Titulo = "Plano Hipertrofia Básico",
                Objetivo = "Hipertrofia",
                CaloriasDiarias = 2800M,
                ProteinasG = 160M,
                CarboidratosG = 350M,
                GordurasG = 80M,
                Orientacoes = "Beber 3.5 litros de água por dia.",
                ObservacoesNutricionista = "Focar na ingestão proteica de fontes limpas.",
                Status = "ativo",
                DataCriacao = DateTime.Now.AddDays(-10),
                DataAtualizacao = DateTime.Now
            };

            var refeicao = new Refeicao
            {
                Id = 40,
                Nome = "Café da Manhã",
                Horario = "08:00",
                Alimentos = "1 copo de leite de amêndoas (zero lactose), 3 ovos mexidos, 2 fatias de pão integral.",
                Calorias = 450,
                PlanoId = 30,
                DataCriacao = DateTime.Now
            };

            plano.Refeicoes.Add(refeicao);
            paciente.Sessoes.Add(sessao);
            paciente.Progressos.Add(progresso);
            paciente.PlanosDieta.Add(plano);

            await db.Pacientes.AddAsync(paciente);
            await db.SaveChangesAsync();

            // 5. Instanciar AIService
            var services = new ServiceCollection();
            services.AddSingleton(db);
            var serviceProvider = services.BuildServiceProvider();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            
            serviceProviderMock.Setup(sp => sp.GetService(typeof(ApplicationDbContext))).Returns(db);
            serviceScopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
            scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(serviceScopeMock.Object);

            var aiService = new AIService(apiKey, scopeFactoryMock.Object, config);

            // 6. Indexar paciente
            _output.WriteLine("Indexando o paciente Lucas Merini...");
            await aiService.IndexPacienteAsync(paciente);
            _output.WriteLine("Indexação concluída!");

            // 7. Fazer perguntas para testar a busca em todas as tabelas
            var perguntas = new[]
            {
                ("Perfil/Saúde (Pacientes)", "Qual é a idade, peso atual e a intolerância do Lucas?"),
                ("Plano de Dieta (Planos_Dieta)", "Qual é o título, objetivo e meta de calorias do plano alimentar ativo do Lucas?"),
                ("Refeições (Refeicoes)", "O que está prescrito no Café da Manhã do Lucas e qual o horário?"),
                ("Sessões (Sessoes)", "O que foi anotado na última sessão de consulta do Lucas e quando ela ocorreu?"),
                ("Progresso (Progresso)", "Como está a aderência, peso e humor do Lucas no último registro de progresso?")
            };

            foreach (var (origem, query) in perguntas)
            {
                _output.WriteLine($"\n--- Testando Tabela de Origem: {origem} ---");
                _output.WriteLine($"Pergunta: {query}");
                
                // Debug individual: Calcular similaridade de cosseno manual para cada chunk
                var embeddingClient = (OpenAI.Embeddings.EmbeddingClient)aiService.GetType()
                    .GetField("_embeddingClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(aiService)!;
                
                var qResult = await embeddingClient.GenerateEmbeddingAsync(query);
                var queryVector = qResult.Value.ToFloats().ToArray();

                _output.WriteLine("Scores Individuais de Similaridade de Cosseno:");
                var allPacienteChunks = aiService.IndexedChunks;
                foreach (var chunk in allPacienteChunks)
                {
                    float cosine = System.Numerics.Tensors.TensorPrimitives.CosineSimilarity(queryVector, chunk.Embedding);
                    _output.WriteLine($" - [{chunk.Tabela}:{chunk.RegistroId}] Tipo: {chunk.Tipo} | Cosine Score: {cosine:F4}");
                }

                var response = await aiService.AskQuestionRagAsync(query, paciente);
                
                _output.WriteLine($"Resposta da IA:\n{response.Answer}");
                _output.WriteLine("Fontes Citadas:");
                if (response.Sources != null && response.Sources.Any())
                {
                    foreach (var src in response.Sources)
                    {
                        _output.WriteLine($" - {src.Citacao} ({src.Tabela} ID={src.RegistroId})");
                    }
                }
                else
                {
                    _output.WriteLine(" - Nenhuma fonte citada.");
                }
            }
        }
    }
}
