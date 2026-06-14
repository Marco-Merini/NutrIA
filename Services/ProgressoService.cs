using NutriFlow.Models;
using NutriFlow.Repositories;
using Microsoft.Extensions.Logging;
using Mapster;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NutriFlow.Services
{
    public class ProgressoService : IProgressoService
    {
        private readonly IProgressoRepository _progressoRepository;
        private readonly ILogger<ProgressoService> _logger;

        public ProgressoService(IProgressoRepository progressoRepository, ILogger<ProgressoService> logger)
        {
            _progressoRepository = progressoRepository;
            _logger = logger;
        }

        public async Task<List<Progresso>> GetProgressosPorPacienteAsync(int pacienteId)
        {
            try
            {
                return await _progressoRepository.GetProgressosPorPacienteAsync(pacienteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar progressos do paciente {PacienteId}", pacienteId);
                return new List<Progresso>();
            }
        }

        public async Task<int> GetProgressosCountPorUsuarioAsync(int usuarioId)
        {
            try
            {
                return await _progressoRepository.GetProgressosCountPorUsuarioAsync(usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar progressos do usuário {UsuarioId}", usuarioId);
                return 0;
            }
        }

        public async Task<Progresso?> GetProgressoByIdAsync(int id)
        {
            try
            {
                return await _progressoRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar progresso {Id}", id);
                return null;
            }
        }

        public async Task<bool> AddProgressoAsync(Progresso progresso)
        {
            try
            {
                progresso.DataCriacao = DateTime.Now;
                progresso.DataAtualizacao = DateTime.Now;
                await _progressoRepository.AddAsync(progresso);
                await _progressoRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar progresso");
                return false;
            }
        }

        public async Task<bool> DeleteProgressoAsync(int id)
        {
            try
            {
                var progresso = await _progressoRepository.GetByIdAsync(id);
                if (progresso == null) return false;

                _progressoRepository.Delete(progresso);
                await _progressoRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar progresso {Id}", id);
                return false;
            }
        }
        public async Task<bool> UpdateProgressoAsync(Progresso progresso)
        {
            try
            {
                var existing = await _progressoRepository.GetByIdAsync(progresso.Id);
                if (existing == null) return false;

                existing.DataRegistro = progresso.DataRegistro;
                existing.Peso = progresso.Peso;
                existing.CinturaCm = progresso.CinturaCm;
                existing.QuadrilCm = progresso.QuadrilCm;
                existing.PercentualGordura = progresso.PercentualGordura;
                existing.AderenciaPlano = progresso.AderenciaPlano;
                existing.Humor = progresso.Humor;
                existing.Energia = progresso.Energia;
                existing.Feedback = progresso.Feedback;
                existing.DataAtualizacao = DateTime.Now;

                await _progressoRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar progresso {Id}", progresso.Id);
                return false;
            }
        }

        public async Task<PaginatedResult<ProgressoResponseDto>> GetProgressosFiltradosAsync(int usuarioId, ProgressoFilter filter)
        {
            try
            {
                var (items, totalCount) = await _progressoRepository.GetProgressosFiltradosAsync(usuarioId, filter);
                
                TypeAdapterConfig<Progresso, ProgressoResponseDto>.NewConfig()
                    .Map(dest => dest.PacienteNome, src => src.Paciente != null ? src.Paciente.Nome : null);

                var dtos = items.Adapt<List<ProgressoResponseDto>>();
                return new PaginatedResult<ProgressoResponseDto>(dtos, totalCount, filter.Page, filter.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar progressos filtrados");
                return new PaginatedResult<ProgressoResponseDto>();
            }
        }
    }
}
