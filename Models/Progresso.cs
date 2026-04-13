using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NutriFlow.Models
{
    [Table("Progresso")]
    public class Progresso
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("paciente_id")]
        public int? PacienteId { get; set; }
        
        [ForeignKey(nameof(PacienteId))]
        public Paciente? Paciente { get; set; }

        [Column("data_registro")]
        public DateTime? DataRegistro { get; set; }
        
        [Column("peso")]
        public decimal? Peso { get; set; }
        
        [Column("cintura_cm")]
        public decimal? CinturaCm { get; set; }
        
        [Column("quadril_cm")]
        public decimal? QuadrilCm { get; set; }
        
        [Column("percentual_gordura")]
        public decimal? PercentualGordura { get; set; }
        
        [Column("aderencia_plano")]
        public string? AderenciaPlano { get; set; }
        
        [Column("humor")]
        public string? Humor { get; set; }
        
        [Column("energia")]
        public string? Energia { get; set; }
        
        [Column("feedback")]
        public string? Feedback { get; set; }
        
        [Column("data_criacao")]
        public DateTime? DataCriacao { get; set; }
        
        [Column("data_atualizacao")]
        public DateTime? DataAtualizacao { get; set; }
    }
}
