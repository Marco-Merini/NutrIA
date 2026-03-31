using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NutriFlow.Models
{
    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("nome")]
        public string? Nome { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("senha_hash")]
        public string? Senha { get; set; }

        [Column("tipo")]
        public string? Tipo { get; set; }

        [Column("ativo")]
        public bool Ativo { get; set; }

        [Column("data_criacao")]
        public DateTime? DataCriacao { get; set; }

        [Column("data_atualizacao")]
        public DateTime? DataAtualizacao { get; set; }

        public ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();
    }
}
