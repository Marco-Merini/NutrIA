using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NutriFlow.Models
{
    /// <summary>
    /// Log auditável de cada consulta à IA assistente.
    /// Armazena userId, pacienteId, query, resposta e fontes para rastreabilidade e conformidade LGPD.
    /// </summary>
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        /// <summary>Id do usuário autenticado (nutricionista) que fez a consulta.</summary>
        [Column("UserId")]
        [MaxLength(200)]
        public string? UserId { get; set; }

        /// <summary>Id do paciente cujos dados foram consultados.</summary>
        [Column("PatientId")]
        public int? PatientId { get; set; }

        /// <summary>Endpoint chamado (ex: "/api/assistente/query").</summary>
        [Column("Endpoint")]
        [MaxLength(200)]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>Texto da query enviada pelo usuário.</summary>
        [Column("QueryText")]
        public string? QueryText { get; set; }

        /// <summary>Resumo da resposta gerada pela IA (primeiros 500 chars).</summary>
        [Column("ResponseSummary")]
        public string? ResponseSummary { get; set; }

        /// <summary>Fontes citadas na resposta (JSON array de SourceReference).</summary>
        [Column("Sources")]
        public string? Sources { get; set; }

        /// <summary>Número de chunks recuperados na busca vetorial.</summary>
        [Column("ChunksRetrieved")]
        public int ChunksRetrieved { get; set; }

        /// <summary>Latência da requisição end-to-end em millisegundos.</summary>
        [Column("LatenciaMs")]
        public int LatenciaMs { get; set; }

        /// <summary>
        /// Indica se o usuário concedeu consentimento LGPD explícito para envio de dados à API OpenAI.
        /// Deve ser true para que a consulta seja processada.
        /// </summary>
        [Column("ConsentimentoLGPD")]
        public bool ConsentimentoLGPD { get; set; }

        /// <summary>Se os dados foram pseudonimizados antes de enviar ao LLM externo.</summary>
        [Column("DadosPseudonimizados")]
        public bool DadosPseudonimizados { get; set; } = true;

        [Column("Timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
