using NutriFlow.Models;
using NutriFlow.Repositories;
using Microsoft.Extensions.Logging;
using Mapster;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NutriFlow.Services
{
    public class PlanoDietaService : IPlanoDietaService
    {
        private readonly IPlanoDietaRepository _planoDietaRepository;
        private readonly ILogger<PlanoDietaService> _logger;

        public PlanoDietaService(IPlanoDietaRepository planoDietaRepository, ILogger<PlanoDietaService> logger)
        {
            _planoDietaRepository = planoDietaRepository;
            _logger = logger;
        }

        public async Task<List<PlanoDieta>> GetPlanosPorPacienteAsync(int pacienteId)
        {
            try
            {
                return await _planoDietaRepository.GetPlanosPorPacienteAsync(pacienteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar planos de dieta do paciente {PacienteId}", pacienteId);
                return new List<PlanoDieta>();
            }
        }

        public async Task<PlanoDieta?> GetPlanoCompletoAsync(int id)
        {
            try
            {
                return await _planoDietaRepository.GetPlanoCompletoAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar plano de dieta completo {Id}", id);
                return null;
            }
        }

        public async Task<int> GetPlanosCountPorUsuarioAsync(int usuarioId)
        {
            try
            {
                return await _planoDietaRepository.GetPlanosCountPorUsuarioAsync(usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar planos de dieta do usuário {UsuarioId}", usuarioId);
                return 0;
            }
        }

        public async Task<bool> AddPlanoDietaAsync(PlanoDieta plano)
        {
            try
            {
                plano.DataCriacao = DateTime.Now;
                plano.DataAtualizacao = DateTime.Now;
                foreach (var refc in plano.Refeicoes)
                {
                    refc.DataCriacao = DateTime.Now;
                }
                await _planoDietaRepository.AddAsync(plano);
                await _planoDietaRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar plano de dieta");
                return false;
            }
        }

        public async Task<bool> DeletePlanoDietaAsync(int id)
        {
            try
            {
                var plano = await _planoDietaRepository.GetByIdAsync(id);
                if (plano == null) return false;

                _planoDietaRepository.Delete(plano);
                await _planoDietaRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar plano de dieta {Id}", id);
                return false;
            }
        }
        public async Task<bool> UpdatePlanoDietaAsync(PlanoDieta plano)
        {
            try
            {
                var existing = await _planoDietaRepository.GetByIdAsync(plano.Id);
                if (existing == null) return false;

                existing.Titulo = plano.Titulo;
                existing.Objetivo = plano.Objetivo;
                existing.CaloriasDiarias = plano.CaloriasDiarias;
                existing.ProteinasG = plano.ProteinasG;
                existing.CarboidratosG = plano.CarboidratosG;
                existing.GordurasG = plano.GordurasG;
                existing.Orientacoes = plano.Orientacoes;
                existing.ObservacoesNutricionista = plano.ObservacoesNutricionista;
                existing.DataAtualizacao = DateTime.Now;

                await _planoDietaRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar plano de dieta {Id}", plano.Id);
                return false;
            }
        }

        public async Task<PaginatedResult<PlanoDietaResponseDto>> GetPlanosFiltradosAsync(int usuarioId, PlanoDietaFilter filter)
        {
            try
            {
                var (items, totalCount) = await _planoDietaRepository.GetPlanosFiltradosAsync(usuarioId, filter);
                
                TypeAdapterConfig<PlanoDieta, PlanoDietaResponseDto>.NewConfig()
                    .Map(dest => dest.PacienteNome, src => src.Paciente != null ? src.Paciente.Nome : null)
                    .Map(dest => dest.RefeicoesCount, src => src.Refeicoes != null ? src.Refeicoes.Count : 0);

                var dtos = items.Adapt<List<PlanoDietaResponseDto>>();
                return new PaginatedResult<PlanoDietaResponseDto>(dtos, totalCount, filter.Page, filter.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar planos filtrados");
                return new PaginatedResult<PlanoDietaResponseDto>();
            }
        }
    }
}
