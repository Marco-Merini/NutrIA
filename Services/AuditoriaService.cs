using NutriFlow.Data;
using NutriFlow.Models;
using NutriFlow.Models.Rag;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace NutriFlow.Services
{
    /// <summary>
    /// Serviço scoped responsável por persistir e consultar logs de auditoria de consultas à IA.
    /// Cada chamada ao endpoint /api/assistente/query gera um registro auditável.
    /// </summary>
    public class AuditoriaService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AuditoriaService> _logger;

        public AuditoriaService(ApplicationDbContext db, ILogger<AuditoriaService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Persiste um log de auditoria de uma consulta RAG.
        /// </summary>
        public async Task LogConsultaAsync(
            string userId,
            int? pacienteId,
            string endpoint,
            string query,
            AssistenteResponse response,
            bool consentimentoLGPD)
        {
            try
            {
                var sourcesJson = response.Sources.Any()
                    ? JsonSerializer.Serialize(response.Sources)
                    : null;

                var log = new AuditLog
                {
                    UserId = userId,
                    PatientId = pacienteId,
                    Endpoint = endpoint,
                    QueryText = query,
                    // Salvar apenas os primeiros 500 caracteres da resposta
                    ResponseSummary = response.Answer.Length > 500
                        ? response.Answer[..500] + "…"
                        : response.Answer,
                    Sources = sourcesJson,
                    ChunksRetrieved = response.Retrieved.Count,
                    LatenciaMs = response.LatenciaMs,
                    ConsentimentoLGPD = consentimentoLGPD,
                    DadosPseudonimizados = true,
                    Timestamp = DateTime.UtcNow
                };

                _db.AuditLogs.Add(log);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "[Auditoria] UserId={UserId} PacienteId={PacienteId} Query={Query} Chunks={Chunks} Latencia={Latencia}ms",
                    userId, pacienteId, query[..Math.Min(80, query.Length)], response.Retrieved.Count, response.LatenciaMs);
            }
            catch (Exception ex)
            {
                // Log sem interromper o fluxo principal
                _logger.LogError(ex, "[Auditoria] Falha ao persistir log de auditoria para userId={UserId}", userId);
            }
        }

        /// <summary>
        /// Retorna logs de auditoria de um paciente, ordenados por data decrescente.
        /// Restringe ao userId para garantir que o nutricionista só veja seus próprios logs.
        /// </summary>
        public async Task<List<AuditLog>> GetLogsPorPacienteAsync(int pacienteId, string userId, int limit = 50)
        {
            return await _db.AuditLogs
                .AsNoTracking()
                .Where(l => l.PatientId == pacienteId && l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Retorna todos os logs de auditoria do usuário (nutricionista).
        /// </summary>
        public async Task<List<AuditLog>> GetLogsPorUsuarioAsync(string userId, int limit = 100)
        {
            return await _db.AuditLogs
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Métricas agregadas: latência média, total de consultas e chunks médios por consulta.
        /// </summary>
        public async Task<AuditoriaMetricas> GetMetricasAsync(string userId)
        {
            var logs = await _db.AuditLogs
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .ToListAsync();

            if (!logs.Any())
                return new AuditoriaMetricas();

            return new AuditoriaMetricas
            {
                TotalConsultas = logs.Count,
                LatenciaMediaMs = (int)logs.Average(l => l.LatenciaMs),
                ChunksMediosPorConsulta = logs.Average(l => l.ChunksRetrieved),
                ConsultasHoje = logs.Count(l => l.Timestamp.Date == DateTime.UtcNow.Date),
                UltimaConsulta = logs.Max(l => l.Timestamp)
            };
        }
    }

    /// <summary>Métricas agregadas de auditoria.</summary>
    public class AuditoriaMetricas
    {
        public int TotalConsultas { get; set; }
        public int LatenciaMediaMs { get; set; }
        public double ChunksMediosPorConsulta { get; set; }
        public int ConsultasHoje { get; set; }
        public DateTime? UltimaConsulta { get; set; }
    }
}
