using NutriFlow.Data;
using NutriFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace NutriFlow.Services
{
    public class PacienteService : IPacienteService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<PacienteService> _logger;

        public PacienteService(ApplicationDbContext dbContext, ILogger<PacienteService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<Paciente>> GetPacientesByUsuarioIdAsync(int usuarioId)
        {
            try
            {
                return await _dbContext.Pacientes
                    .AsNoTracking()
                    .Where(p => p.UsuarioId == usuarioId)
                    .OrderBy(p => p.Nome)
                    .ToListAsync();
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
                return await _dbContext.Pacientes
                    .Include(p => p.Sessoes)
                    .Include(p => p.Progressos)
                    .Include(p => p.PlanosDieta)
                        .ThenInclude(pd => pd.Refeicoes)
                    .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar paciente {Id} do usuário {UsuarioId}", id, usuarioId);
                return null;
            }
        }

        public async Task<bool> AddPacienteAsync(Paciente paciente)
        {
            try
            {
                paciente.DataCadastro = DateTime.Now;
                paciente.DataAtualizacao = DateTime.Now;
                paciente.Status = "ativo";
                
                await _dbContext.Pacientes.AddAsync(paciente);
                await _dbContext.SaveChangesAsync();
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
                var existing = await _dbContext.Pacientes.FirstOrDefaultAsync(p => p.Id == paciente.Id && p.UsuarioId == paciente.UsuarioId);
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

                _dbContext.Pacientes.Update(existing);
                await _dbContext.SaveChangesAsync();
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
                var paciente = await _dbContext.Pacientes.FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuarioId);
                if (paciente == null) return false;

                _dbContext.Pacientes.Remove(paciente);
                await _dbContext.SaveChangesAsync();
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
