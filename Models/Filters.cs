using System;

namespace NutriFlow.Models
{
    public class BaseFilter
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public string SortDirection { get; set; } = "desc"; // "asc" or "desc"
    }

    public class PlanoDietaFilter : BaseFilter
    {
        public int? PacienteId { get; set; }
        public string? Nome { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
    }

    public class ProgressoFilter : BaseFilter
    {
        public int? PacienteId { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
    }

    public class SessaoFilter : BaseFilter
    {
        public int? PacienteId { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
    }
}
