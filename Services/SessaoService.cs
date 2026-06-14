using NutriFlow.Models;
using NutriFlow.Repositories;
using Microsoft.Extensions.Logging;
using Mapster;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NutriFlow.Services
{
    public class SessaoService : ISessaoService
    {
        private readonly ISessaoRepository _sessaoRepository;
        private readonly ILogger<SessaoService> _logger;

        public SessaoService(ISessaoRepository sessaoRepository, ILogger<SessaoService> logger)
        {
            _sessaoRepository = sessaoRepository;
            _logger = logger;
        }

        public async Task<List<Sessao>> GetSessoesPorUsuarioAsync(int usuarioId)
        {
            try
            {
                return await _sessaoRepository.GetSessoesPorUsuarioAsync(usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar sessões do usuário {UsuarioId}", usuarioId);
                return new List<Sessao>();
            }
        }

        public async Task<List<Sessao>> GetSessoesRecentesPorUsuarioAsync(int usuarioId, int count)
        {
            try
            {
                return await _sessaoRepository.GetSessoesRecentesPorUsuarioAsync(usuarioId, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar sessões recentes do usuário {UsuarioId}", usuarioId);
                return new List<Sessao>();
            }
        }

        public async Task<int> GetSessoesCountPorUsuarioAsync(int usuarioId)
        {
            try
            {
                return await _sessaoRepository.GetSessoesCountPorUsuarioAsync(usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar sessões do usuário {UsuarioId}", usuarioId);
                return 0;
            }
        }

        public async Task<Sessao?> GetSessaoByIdAsync(int id)
        {
            try
            {
                return await _sessaoRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar sessão {Id}", id);
                return null;
            }
        }

        public async Task<bool> AddSessaoAsync(Sessao sessao)
        {
            try
            {
                sessao.DataCriacao = DateTime.Now;
                sessao.DataAtualizacao = DateTime.Now;
                await _sessaoRepository.AddAsync(sessao);
                await _sessaoRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar sessão");
                return false;
            }
        }

        public async Task<bool> DeleteSessaoAsync(int id)
        {
            try
            {
                var sessao = await _sessaoRepository.GetByIdAsync(id);
                if (sessao == null) return false;

                _sessaoRepository.Delete(sessao);
                await _sessaoRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar sessão {Id}", id);
                return false;
            }
        }
        public async Task<bool> UpdateSessaoAsync(Sessao sessao)
        {
            try
            {
                var existing = await _sessaoRepository.GetByIdAsync(sessao.Id);
                if (existing == null) return false;

                existing.DataSessao = sessao.DataSessao;
                existing.Tipo = sessao.Tipo;
                existing.PesoSessao = sessao.PesoSessao;
                existing.Anotacoes = sessao.Anotacoes;
                existing.ProximaConsulta = sessao.ProximaConsulta;
                existing.DataAtualizacao = DateTime.Now;

                await _sessaoRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar sessão {Id}", sessao.Id);
                return false;
            }
        }

        public async Task<PaginatedResult<SessaoResponseDto>> GetSessoesFiltradasAsync(int usuarioId, SessaoFilter filter)
        {
            try
            {
                var (items, totalCount) = await _sessaoRepository.GetSessoesFiltradasAsync(usuarioId, filter);
                
                TypeAdapterConfig<Sessao, SessaoResponseDto>.NewConfig()
                    .Map(dest => dest.PacienteNome, src => src.Paciente != null ? src.Paciente.Nome : null);

                var dtos = items.Adapt<List<SessaoResponseDto>>();
                return new PaginatedResult<SessaoResponseDto>(dtos, totalCount, filter.Page, filter.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar sessões filtradas");
                return new PaginatedResult<SessaoResponseDto>();
            }
        }
    }
}
