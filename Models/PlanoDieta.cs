namespace NutriFlow.Models
{
    public class PlanoDieta
    {
        public int Id { get; set; }
        public int PacienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int CaloriasDiarias { get; set; }
        public double ProteinasG { get; set; }
        public double CarboidratosG { get; set; }
        public double GordurasG { get; set; }
        public string Status { get; set; } = "ativo";
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string Orientacoes { get; set; } = string.Empty;
        public string ObservacoesNutricionista { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime DataAtualizacao { get; set; }

        // Propriedades de navegação
        public virtual Paciente? Paciente { get; set; }
        public virtual ICollection<Refeicao>? Refeicoes { get; set; }
    }
}
