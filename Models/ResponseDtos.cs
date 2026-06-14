using System;

namespace NutriFlow.Models
{
    public class PlanoDietaResponseDto
    {
        public int Id { get; set; }
        public int? PacienteId { get; set; }
        public string? PacienteNome { get; set; }
        public string? Titulo { get; set; }
        public string? Objetivo { get; set; }
        public decimal? CaloriasDiarias { get; set; }
        public decimal? ProteinasG { get; set; }
        public decimal? CarboidratosG { get; set; }
        public decimal? GordurasG { get; set; }
        public string? Orientacoes { get; set; }
        public string? ObservacoesNutricionista { get; set; }
        public string? Status { get; set; }
        public DateTime? DataCriacao { get; set; }
        public DateTime? DataAtualizacao { get; set; }
        public int RefeicoesCount { get; set; }
    }

    public class ProgressoResponseDto
    {
        public int Id { get; set; }
        public int? PacienteId { get; set; }
        public string? PacienteNome { get; set; }
        public DateTime? DataRegistro { get; set; }
        public decimal? Peso { get; set; }
        public decimal? CinturaCm { get; set; }
        public decimal? QuadrilCm { get; set; }
        public decimal? PercentualGordura { get; set; }
        public string? AderenciaPlano { get; set; }
        public string? Humor { get; set; }
        public string? Energia { get; set; }
        public string? Feedback { get; set; }
        public DateTime? DataCriacao { get; set; }
        public DateTime? DataAtualizacao { get; set; }
    }

    public class SessaoResponseDto
    {
        public int Id { get; set; }
        public int? PacienteId { get; set; }
        public string? PacienteNome { get; set; }
        public DateTime? DataSessao { get; set; }
        public string? Tipo { get; set; }
        public decimal? PesoSessao { get; set; }
        public string? Anotacoes { get; set; }
        public DateTime? ProximaConsulta { get; set; }
        public DateTime? DataCriacao { get; set; }
        public DateTime? DataAtualizacao { get; set; }
    }
}
