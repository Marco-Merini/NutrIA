using Microsoft.Extensions.Logging;
using Moq;
using NutriFlow.Models;
using NutriFlow.Models.Rag;
using NutriFlow.Repositories;
using NutriFlow.Services;
using Xunit;

namespace NutriFlow.Tests
{
    public class AuditoriaServiceTests
    {
        [Fact]
        public async Task LogConsultaAsync_PersistsTruncatedAuditLogWithSources()
        {
            var repo = new Mock<IAuditLogRepository>();
            var service = new AuditoriaService(repo.Object, Mock.Of<ILogger<AuditoriaService>>());
            AuditLog? captured = null;
            var longAnswer = new string('a', 520);
            var response = new AssistenteResponse
            {
                Answer = longAnswer,
                LatenciaMs = 123,
                Sources = new List<SourceReference>
                {
                    new() { Tabela = "Sessoes", RegistroId = 5, Citacao = "[Sessoes:5]" }
                },
                Retrieved = new List<RetrievedChunk>
                {
                    new() { Source = "Sessoes:5", Tabela = "Sessoes", RegistroId = 5 }
                }
            };

            repo.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => captured = log)
                .Returns(Task.CompletedTask);

            await service.LogConsultaAsync("42", 7, "/api/v1/assistente/query", "query clinica", response, true);

            Assert.NotNull(captured);
            Assert.Equal("42", captured.UserId);
            Assert.Equal(7, captured.PatientId);
            Assert.Equal("/api/v1/assistente/query", captured.Endpoint);
            Assert.Equal("query clinica", captured.QueryText);
            Assert.Equal(501, captured.ResponseSummary?.Length);
            Assert.Equal(1, captured.ChunksRetrieved);
            Assert.Equal(123, captured.LatenciaMs);
            Assert.True(captured.ConsentimentoLGPD);
            Assert.True(captured.DadosPseudonimizados);
            Assert.Contains("Sessoes", captured.Sources);
            repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task QueryMethods_ReturnLogsAndFallbackToEmptyLists()
        {
            var repo = new Mock<IAuditLogRepository>();
            var service = new AuditoriaService(repo.Object, Mock.Of<ILogger<AuditoriaService>>());
            var logs = new List<AuditLog> { new() { Id = 1, UserId = "u1", PatientId = 3 } };

            repo.Setup(r => r.GetLogsPorPacienteAsync(3, "u1", 50)).ReturnsAsync(logs);
            repo.Setup(r => r.GetLogsPorUsuarioAsync("u1", 100)).ReturnsAsync(logs);

            Assert.Same(logs, await service.GetLogsPorPacienteAsync(3, "u1"));
            Assert.Same(logs, await service.GetLogsPorUsuarioAsync("u1"));

            repo.Setup(r => r.GetLogsPorPacienteAsync(4, "u1", 50)).ThrowsAsync(new InvalidOperationException());
            repo.Setup(r => r.GetLogsPorUsuarioAsync("erro", 100)).ThrowsAsync(new InvalidOperationException());

            Assert.Empty(await service.GetLogsPorPacienteAsync(4, "u1"));
            Assert.Empty(await service.GetLogsPorUsuarioAsync("erro"));
        }

        [Fact]
        public async Task GetMetricasAsync_CalculatesAggregatedValuesAndFallbacks()
        {
            var repo = new Mock<IAuditLogRepository>();
            var service = new AuditoriaService(repo.Object, Mock.Of<ILogger<AuditoriaService>>());
            var today = DateTime.UtcNow;
            var logs = new List<AuditLog>
            {
                new() { UserId = "u1", LatenciaMs = 100, ChunksRetrieved = 2, Timestamp = today },
                new() { UserId = "u1", LatenciaMs = 300, ChunksRetrieved = 4, Timestamp = today.AddDays(-1) }
            };

            repo.Setup(r => r.GetTodosLogsPorUsuarioAsync("u1")).ReturnsAsync(logs);
            repo.Setup(r => r.GetTodosLogsPorUsuarioAsync("empty")).ReturnsAsync(new List<AuditLog>());
            repo.Setup(r => r.GetTodosLogsPorUsuarioAsync("erro")).ThrowsAsync(new InvalidOperationException());

            var metricas = await service.GetMetricasAsync("u1");

            Assert.Equal(2, metricas.TotalConsultas);
            Assert.Equal(200, metricas.LatenciaMediaMs);
            Assert.Equal(3, metricas.ChunksMediosPorConsulta);
            Assert.Equal(1, metricas.ConsultasHoje);
            Assert.Equal(logs.Max(l => l.Timestamp), metricas.UltimaConsulta);

            Assert.Equal(0, (await service.GetMetricasAsync("empty")).TotalConsultas);
            Assert.Equal(0, (await service.GetMetricasAsync("erro")).TotalConsultas);
        }
    }
}
