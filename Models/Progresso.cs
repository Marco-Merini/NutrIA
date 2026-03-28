namespace NutriFlow.Models
{
    public class Progresso
    {
        public int Id { get; set; }
        public int PacienteId { get; set; }
        public DateTime Data { get; set; }
        public double PesoAtual { get; set; }
        public double PesoObjetivo { get; set; }
        public string Observacoes { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime DataAtualizacao { get; set; }

        // Propriedades de navegação
        public virtual Paciente? Paciente { get; set; }
    }
}
