using NutriFlow.Models;
using NutriFlow.Models.Rag;
using NutriFlow.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NutriFlow.Services
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<AuditoriaService> _logger;

        public AuditoriaService(IAuditLogRepository auditLogRepository, ILogger<AuditoriaService> logger)
        {
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

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

                await _auditLogRepository.AddAsync(log);
                await _auditLogRepository.SaveChangesAsync();

                _logger.LogInformation(
                    "[Auditoria] UserId={UserId} PacienteId={PacienteId} Query={Query} Chunks={Chunks} Latencia={Latencia}ms",
                    userId, pacienteId, query[..Math.Min(80, query.Length)], response.Retrieved.Count, response.LatenciaMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Auditoria] Falha ao persistir log de auditoria para userId={UserId}", userId);
            }
        }

        public async Task<List<AuditLog>> GetLogsPorPacienteAsync(int pacienteId, string userId, int limit = 50)
        {
            try
            {
                return await _auditLogRepository.GetLogsPorPacienteAsync(pacienteId, userId, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logs de auditoria por paciente {PacienteId}", pacienteId);
                return new List<AuditLog>();
            }
        }

        public async Task<List<AuditLog>> GetLogsPorUsuarioAsync(string userId, int limit = 100)
        {
            try
            {
                return await _auditLogRepository.GetLogsPorUsuarioAsync(userId, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logs de auditoria por usuário {UserId}", userId);
                return new List<AuditLog>();
            }
        }

        public async Task<AuditoriaMetricas> GetMetricasAsync(string userId)
        {
            try
            {
                var logs = await _auditLogRepository.GetTodosLogsPorUsuarioAsync(userId);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular métricas de auditoria para o usuário {UserId}", userId);
                return new AuditoriaMetricas();
            }
        }
    }
}
