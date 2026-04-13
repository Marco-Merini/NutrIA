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
        public string? Horario { get; set; }
        
        [Column("alimentos")]
        public string? Alimentos { get; set; }
        
        [Column("calorias")]
        public decimal? Calorias { get; set; }
        
        [Column("data_criacao")]
        public DateTime? DataCriacao { get; set; }
    }
}
