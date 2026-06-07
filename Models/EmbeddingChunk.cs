using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NutriFlow.Models
{
    /// <summary>
    /// Entidade persistida no SQL Server para armazenamento de embeddings vetoriais.
    /// Permite re-uso do índice após reinício do servidor sem necessidade de re-indexação.
    /// </summary>
    [Table("Embeddings")]
    public class EmbeddingChunk
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        /// <summary>Id do paciente ao qual este chunk pertence. Null = chunk global.</summary>
        [Column("PatientId")]
        public int? PatientId { get; set; }

        /// <summary>Nome da tabela de origem (ex: "Pacientes", "Planos_Dieta", "Progresso", "Sessoes", "Refeicoes").</summary>
        [Column("SourceTable")]
        [MaxLength(100)]
        public string SourceTable { get; set; } = string.Empty;

        /// <summary>Id do registro de origem na tabela correspondente.</summary>
        [Column("SourceId")]
        public int? SourceId { get; set; }

        /// <summary>Tipo semântico do chunk (perfil, preferencias, saude, medicamentos, sessao, progresso, plano, refeicao).</summary>
        [Column("ChunkType")]
        [MaxLength(50)]
        public string ChunkType { get; set; } = string.Empty;

        /// <summary>Texto original do chunk, gerado pela normalização dos dados relacionais.</summary>
        [Column("ChunkText")]
        public string ChunkText { get; set; } = string.Empty;

        /// <summary>Embedding serializado como JSON array de floats (1536 dimensões para text-embedding-3-small).</summary>
        [Column("EmbeddingJson")]
        public string EmbeddingJson { get; set; } = string.Empty;

        /// <summary>Data de criação/última atualização do registro de origem.</summary>
        [Column("SourceUpdatedAt")]
        public DateTime? SourceUpdatedAt { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}
