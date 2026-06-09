using NutriFlow.Models;
using NutriFlow.Models.Rag;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace NutriFlow.Services
{
    public interface IAuditoriaService
    {
        Task LogConsultaAsync(
            string userId,
            int? pacienteId,
            string endpoint,
            string query,
            AssistenteResponse response,
            bool consentimentoLGPD);

        Task<List<AuditLog>> GetLogsPorPacienteAsync(int pacienteId, string userId, int limit = 50);
        Task<List<AuditLog>> GetLogsPorUsuarioAsync(string userId, int limit = 100);
        Task<AuditoriaMetricas> GetMetricasAsync(string userId);
    }

    public class AuditoriaMetricas
    {
        public int TotalConsultas { get; set; }
        public int LatenciaMediaMs { get; set; }
        public double ChunksMediosPorConsulta { get; set; }
        public int ConsultasHoje { get; set; }
        public DateTime? UltimaConsulta { get; set; }
    }
}
