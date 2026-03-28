namespace NutriFlow.Models
{
    public class Sessao
    {
        public int Id { get; set; }
        public int PacienteId { get; set; }
        public DateTime Data { get; set; }
        public TimeSpan Horario { get; set; }
        public string Tipo { get; set; } = string.Empty; // consulta_inicial, retorno, avaliacao, online
        public string Observacoes { get; set; } = string.Empty;
        public string Status { get; set; } = "agendada"; // agendada, realizada, cancelada
        public DateTime DataCriacao { get; set; }
        public DateTime DataAtualizacao { get; set; }

        // Propriedades de navegação
        public virtual Paciente? Paciente { get; set; }
    }
}
