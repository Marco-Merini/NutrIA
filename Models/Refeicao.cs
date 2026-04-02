using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NutriFlow.Models
{
    [Table("Refeicoes")]
    public class Refeicao
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("plano_id")]
        public int? PlanoId { get; set; }
        
        [ForeignKey(nameof(PlanoId))]
        public PlanoDieta? PlanoDieta { get; set; }

        [Column("nome")]
        public string? Nome { get; set; }
        
        [Column("horario")]
        public TimeSpan? Horario { get; set; }
        
        [Column("alimentos")]
        public string? Alimentos { get; set; }
        
        [Column("calorias")]
        public int? Calorias { get; set; }
        
        [Column("data_criacao")]
        public DateTime? DataCriacao { get; set; }
    }
}
