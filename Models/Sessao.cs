using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NutriFlow.Models
{
    [Table("Sessoes")]
    public class Sessao
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("paciente_id")]
        public int? PacienteId { get; set; }
        
        [ForeignKey(nameof(PacienteId))]
        public Paciente? Paciente { get; set; }

        [Column("data_sessao")]
        public DateTime? DataSessao { get; set; }
        
        [Column("tipo")]
        public string? Tipo { get; set; }
        
        [Column("peso_sessao")]
        public decimal? PesoSessao { get; set; }
        
        [Column("anotacoes")]
        public string? Anotacoes { get; set; }
        
        [Column("proxima_consulta")]
        public DateTime? ProximaConsulta { get; set; }
        
        [Column("data_criacao")]
        public DateTime? DataCriacao { get; set; }
        
        [Column("data_atualizacao")]
        public DateTime? DataAtualizacao { get; set; }
    }
}
