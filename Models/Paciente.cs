using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NutriFlow.Models
{
    [Table("Pacientes")]
    public class Paciente
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("usuario_id")]
        public int? UsuarioId { get; set; }
        
        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        [Column("nome")]
        public string? Nome { get; set; }
        
        [Column("data_nascimento")]
        public DateTime? DataNascimento { get; set; }
        
        [Column("sexo")]
        public string? Sexo { get; set; }
        
        [Column("email")]
        public string? Email { get; set; }
        
        [Column("telefone")]
        public string? Telefone { get; set; }
        
        [Column("peso_atual")]
        public decimal? PesoAtual { get; set; }
        
        [Column("altura")]
        public decimal? Altura { get; set; }
        
        [Column("objetivo")]
        public string? Objetivo { get; set; }
        
        [Column("nivel_atividade")]
        public string? NivelAtividade { get; set; }
        
        [Column("preferencias_alimentares")]
        public string? PreferenciasAlimentares { get; set; }
        
        [Column("condicoes_saude")]
        public string? CondicoesSaude { get; set; }
        
        [Column("medicamentos")]
        public string? Medicamentos { get; set; }
        
        [Column("observacoes")]
        public string? Observacoes { get; set; }
        
        [Column("status")]
        public string? Status { get; set; }
        
        [Column("data_cadastro")]
        public DateTime? DataCadastro { get; set; }
        
        [Column("data_atualizacao")]
        public DateTime? DataAtualizacao { get; set; }

        public ICollection<PlanoDieta> PlanosDieta { get; set; } = new List<PlanoDieta>();
        public ICollection<Sessao> Sessoes { get; set; } = new List<Sessao>();
        public ICollection<Progresso> Progressos { get; set; } = new List<Progresso>();
    }
}
