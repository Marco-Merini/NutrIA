namespace NutriFlow.Models.Rag
{
    /// <summary>
    /// Chunk de documento com metadados estruturados para o sistema RAG.
    /// Substitui a classe DocumentChunk inline em AIService.cs com suporte a
    /// metadados ricos (tabela, registroId, tipo, score) para citações de fontes.
    /// </summary>
    public class DocumentChunk
    {
        /// <summary>Índice sequencial no índice em memória.</summary>
        public int Id { get; set; }

        /// <summary>Identificador legível da fonte, ex: "Sessoes:42".</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>Nome da tabela de origem (Pacientes, Planos_Dieta, Progresso, Sessoes, Refeicoes).</summary>
        public string Tabela { get; set; } = string.Empty;

        /// <summary>Id do registro na tabela de origem.</summary>
        public int? RegistroId { get; set; }

        /// <summary>Id do paciente ao qual este chunk pertence.</summary>
        public int? PacienteId { get; set; }

        /// <summary>
        /// Tipo semântico: perfil | preferencias | saude | medicamentos | observacoes |
        ///                 sessao | progresso | plano | refeicao
        /// </summary>
        public string Tipo { get; set; } = string.Empty;

        /// <summary>Data de atualização do registro de origem (para reranking por recência).</summary>
        public DateTime? DataAtualizacao { get; set; }

        /// <summary>Texto normalizado do chunk que foi embeddado.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Vetor de embedding (1536 dims para text-embedding-3-small).</summary>
        public float[] Embedding { get; set; } = Array.Empty<float>();

        /// <summary>Score de similaridade retornado pela busca vetorial (0-1, Cosine).</summary>
        public float Score { get; set; }

        /// <summary>Citação no formato [Tabela:Id] para incluir nas respostas.</summary>
        public string Citacao => $"[{Tabela}:{RegistroId}]";
    }
}
