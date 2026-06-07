namespace NutriFlow.Models.Rag
{
    /// <summary>
    /// Parâmetros configuráveis do pipeline RAG.
    /// Valores padrão lidos do appsettings.json seção "RAG".
    /// Podem ser sobrescritos por request via AssistenteQuery.Options.
    /// </summary>
    public class RagOptions
    {
        /// <summary>Número de chunks a recuperar na busca vetorial (top-K). Default: 8.</summary>
        public int TopK { get; set; } = 8;

        /// <summary>
        /// Score mínimo de similaridade coseno para incluir um chunk (0-1).
        /// Chunks abaixo do threshold são descartados. Default: 0.25.
        /// </summary>
        public float ScoreThreshold { get; set; } = 0.25f;

        /// <summary>Se true, inclui chunks de refeições na busca. Default: true.</summary>
        public bool IncluirRefeicoes { get; set; } = true;

        /// <summary>Meta calórica diária (kcal) para geração de dieta. Null = usar dados do paciente.</summary>
        public decimal? MetaCaloriasDia { get; set; }

        /// <summary>Número de refeições por dia para geração de dieta. Default: 3.</summary>
        public int NumRefeicoesDia { get; set; } = 3;

        /// <summary>Número de lanches por dia para geração de dieta. Default: 2.</summary>
        public int NumLanchesDia { get; set; } = 2;

        /// <summary>Preferências alimentares adicionais especificadas na query (ex: "sem glúten").</summary>
        public string? PreferenciasExtras { get; set; }

        /// <summary>Meta do plano: "perda" | "ganho" | "manutencao". Null = usar objetivo do paciente.</summary>
        public string? MetaPlano { get; set; }
    }
}
