using NutriFlow.Models;
using NutriFlow.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NutriFlow.Services
{
    public class PacienteService : IPacienteService
    {
        private readonly IPacienteRepository _pacienteRepository;
        private readonly ILogger<PacienteService> _logger;

        public PacienteService(IPacienteRepository pacienteRepository, ILogger<PacienteService> logger)
        {
            _pacienteRepository = pacienteRepository;
            _logger = logger;
        }

        public async Task<List<Paciente>> GetPacientesByUsuarioIdAsync(int usuarioId)
        {
            try
            {
                return await _pacienteRepository.GetPacientesByUsuarioIdAsync(usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar pacientes do usuário {UsuarioId}", usuarioId);
                return new List<Paciente>();
            }
        }

        public async Task<Paciente?> GetPacienteByIdAsync(int id, int usuarioId)
        {
            try
            {
                return await _pacienteRepository.GetPacienteCompletoAsync(id, usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar paciente {Id} do usuário {UsuarioId}", id, usuarioId);
                return null;
            }
        }

        public async Task<List<Paciente>> GetPacientesRecentesAsync(int usuarioId, int count)
        {
            try
            {
                return await _pacienteRepository.GetPacientesRecentesAsync(usuarioId, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar pacientes recentes do usuário {UsuarioId}", usuarioId);
                return new List<Paciente>();
            }
        }

        public async Task<bool> AddPacienteAsync(Paciente paciente)
        {
            try
            {
                paciente.DataCadastro = DateTime.Now;
                paciente.DataAtualizacao = DateTime.Now;
                paciente.Status = "ativo";
                
                await _pacienteRepository.AddAsync(paciente);
                await _pacienteRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar paciente");
                return false;
            }
        }

        public async Task<bool> UpdatePacienteAsync(Paciente paciente)
        {
            try
            {
                if (paciente.UsuarioId == null) return false;
                var existing = await _pacienteRepository.GetPacienteCompletoAsync(paciente.Id, paciente.UsuarioId.Value);
                if (existing == null) return false;

                existing.Nome = paciente.Nome;
                existing.DataNascimento = paciente.DataNascimento;
                existing.Sexo = paciente.Sexo;
                existing.Email = paciente.Email;
                existing.Telefone = paciente.Telefone;
                existing.PesoAtual = paciente.PesoAtual;
                existing.Altura = paciente.Altura;
                existing.Objetivo = paciente.Objetivo;
                existing.NivelAtividade = paciente.NivelAtividade;
                existing.PreferenciasAlimentares = paciente.PreferenciasAlimentares;
                existing.CondicoesSaude = paciente.CondicoesSaude;
                existing.Medicamentos = paciente.Medicamentos;
                existing.Observacoes = paciente.Observacoes;
                existing.Status = paciente.Status;
                existing.DataAtualizacao = DateTime.Now;

                await _pacienteRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar paciente {Id}", paciente.Id);
                return false;
            }
        }

        public async Task<bool> DeletePacienteAsync(int id, int usuarioId)
        {
            try
            {
                var paciente = await _pacienteRepository.GetPacienteCompletoAsync(id, usuarioId);
                if (paciente == null) return false;

                _pacienteRepository.Delete(paciente);
                await _pacienteRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar paciente {Id} do usuário {UsuarioId}", id, usuarioId);
                return false;
            }
        }
    }
}
