namespace NutriFlow.Models
{
    public class Refeicao
    {
        public int Id { get; set; }
        public int PlanoDietaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public TimeSpan Horario { get; set; }
        public string Alimentos { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int Calorias { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime DataAtualizacao { get; set; }

        // Propriedades de navegação
        public virtual PlanoDieta? PlanoDieta { get; set; }
    }
}
