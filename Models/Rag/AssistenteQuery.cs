namespace NutriFlow.Models.Rag
{
    /// <summary>
    /// DTO de entrada do endpoint POST /api/assistente/query.
    /// </summary>
    public class AssistenteQuery
    {
        /// <summary>Id do usuário autenticado (nutricionista). Extraído dos claims JWT no server-side.</summary>
        public int UserId { get; set; }

        /// <summary>Id do paciente cujos dados devem ser consultados.</summary>
        public int PacienteId { get; set; }

        /// <summary>Id de sessão de conversa (opcional, para contexto multi-turno futuro).</summary>
        public string? SessionId { get; set; }

        /// <summary>Pergunta ou instrução do usuário.</summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Consentimento LGPD explícito do usuário para envio de dados clínicos pseudonimizados
        /// à API OpenAI. A consulta só é processada se true.
        /// </summary>
        public bool ConsentimentoLGPD { get; set; }

        /// <summary>Parâmetros configuráveis do pipeline RAG (opcional, usa defaults do appsettings).</summary>
        public RagOptions? Options { get; set; }
    }
}
