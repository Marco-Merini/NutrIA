namespace NutriFlow.Models.Rag
{
    /// <summary>
    /// DTO de saída do endpoint POST /api/assistente/query.
    /// Contém resposta, fontes citadas e chunks recuperados para auditoria e exibição no UI.
    /// </summary>
    public class AssistenteResponse
    {
        /// <summary>Resposta gerada pelo LLM com citações de fontes no formato [Tabela:Id].</summary>
        public string Answer { get; set; } = string.Empty;

        /// <summary>Lista de fontes únicas citadas na resposta (para renderizar chips no UI).</summary>
        public List<SourceReference> Sources { get; set; } = new();

        /// <summary>Chunks recuperados pela busca vetorial (para "Ver entidades utilizadas").</summary>
        public List<RetrievedChunk> Retrieved { get; set; } = new();

        /// <summary>Latência end-to-end da requisição em millisegundos.</summary>
        public int LatenciaMs { get; set; }

        /// <summary>Indica se a resposta foi gerada com base em dados indexados.</summary>
        public bool UsouDadosIndexados { get; set; }

        /// <summary>Mensagem de erro, se houver.</summary>
        public string? Error { get; set; }
    }

    /// <summary>
    /// Referência a uma fonte específica citada na resposta.
    /// </summary>
    public class SourceReference
    {
        /// <summary>Nome da tabela (ex: "Planos_Dieta").</summary>
        public string Tabela { get; set; } = string.Empty;

        /// <summary>Id do registro na tabela.</summary>
        public int RegistroId { get; set; }

        /// <summary>Descrição legível da fonte.</summary>
        public string Descricao { get; set; } = string.Empty;

        /// <summary>Citação formatada, ex: "[PlanosDieta:45]".</summary>
        public string Citacao { get; set; } = string.Empty;
    }

    /// <summary>
    /// Preview de um chunk recuperado pela busca vetorial.
    /// Exibido em "Ver entidades utilizadas" no UI.
    /// </summary>
    public class RetrievedChunk
    {
        public string Source { get; set; } = string.Empty;
        public string Tabela { get; set; } = string.Empty;
        public int? RegistroId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public float Score { get; set; }

        /// <summary>Preview dos primeiros 300 caracteres do chunk.</summary>
        public string TextPreview { get; set; } = string.Empty;
    }
}
