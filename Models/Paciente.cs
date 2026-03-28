namespace NutriFlow.Models
{
    public class Paciente
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public DateTime DataNascimento { get; set; }
        public string Sexo { get; set; } = string.Empty;
        public double Peso { get; set; }
        public double Altura { get; set; }
        public string Objetivo { get; set; } = string.Empty;
        public string NivelAtividade { get; set; } = string.Empty;
        public string PreferenciasAlimentares { get; set; } = string.Empty;
        public string CondicoesSaude { get; set; } = string.Empty;
        public string Medicamentos { get; set; } = string.Empty;
        public string Observacoes { get; set; } = string.Empty;
        public string Status { get; set; } = "ativo";
        public DateTime DataCriacao { get; set; }
        public DateTime DataAtualizacao { get; set; }

        // Propriedades de navegação
        public virtual ICollection<PlanoDieta>? PlanosDieta { get; set; }
        public virtual ICollection<Sessao>? Sessoes { get; set; }
        public virtual ICollection<Progresso>? Progressos { get; set; }
    }
}
