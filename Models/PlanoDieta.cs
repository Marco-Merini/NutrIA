using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NutriFlow.Models
{
    [Table("Planos_Dieta")]
    public class PlanoDieta
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("paciente_id")]
        public int? PacienteId { get; set; }
        
        [ForeignKey(nameof(PacienteId))]
        public Paciente? Paciente { get; set; }

        [Column("titulo")]
        public string? Titulo { get; set; }
        
        [Column("objetivo")]
        public string? Objetivo { get; set; }
        
        [Column("calorias_diarias")]
        public int? CaloriasDiarias { get; set; }
        
        [Column("proteinas_g")]
        public decimal? ProteinasG { get; set; }
        
        [Column("carboidratos_g")]
        public decimal? CarboidratosG { get; set; }
        
        [Column("gorduras_g")]
        public decimal? GordurasG { get; set; }
        
        [Column("orientacoes")]
        public string? Orientacoes { get; set; }
        
        [Column("observacoes_nutricionista")]
        public string? ObservacoesNutricionista { get; set; }
        
        [Column("status")]
        public string? Status { get; set; }
        
        [Column("gerado_por_ia")]
        public bool? GeradoPorIa { get; set; }
        
        [Column("data_criacao")]
        public DateTime? DataCriacao { get; set; }
        
        [Column("data_atualizacao")]
        public DateTime? DataAtualizacao { get; set; }

        public ICollection<Refeicao> Refeicoes { get; set; } = new List<Refeicao>();
    }
}
