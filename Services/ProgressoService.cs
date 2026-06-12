using NutriFlow.Models;
using NutriFlow.Repositories;
using Microsoft.Extensions.Logging;
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
    }
}
