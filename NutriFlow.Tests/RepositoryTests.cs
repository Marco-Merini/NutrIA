using Microsoft.EntityFrameworkCore;
using NutriFlow.Data;
using NutriFlow.Models;
using NutriFlow.Repositories;
using Xunit;

namespace NutriFlow.Tests
{
    public class RepositoryTests
    {
        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task BaseRepository_AddsUpdatesDeletesAndReadsEntities()
        {
            await using var context = CreateContext();
            var repository = new Repository<Usuario>(context);
            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Nutri",
                Email = "nutri@test.com",
                Senha = "hash",
                Ativo = "S"
            };

            await repository.AddAsync(usuario);
            await repository.SaveChangesAsync();

            var saved = await repository.GetByIdAsync(1);
            Assert.NotNull(saved);
            Assert.Single(await repository.GetAllAsync());

            saved.Nome = "Nutri Atualizado";
            repository.Update(saved);
            await repository.SaveChangesAsync();
            Assert.Equal("Nutri Atualizado", (await repository.GetByIdAsync(1))?.Nome);

            repository.Delete(saved);
            await repository.SaveChangesAsync();
            Assert.Empty(await repository.GetAllAsync());
        }

        [Fact]
        public async Task PacienteRepository_ReturnsOnlyUserPatientsAndIncludesRelations()
        {
            await using var context = CreateContext();
            context.Pacientes.AddRange(
                new Paciente
                {
                    Id = 1,
                    UsuarioId = 10,
                    Nome = "Bia",
                    DataCadastro = DateTime.Today.AddDays(-1),
                    Sessoes = new List<Sessao> { new() { Id = 1 } },
                    Progressos = new List<Progresso> { new() { Id = 1 } },
                    PlanosDieta = new List<PlanoDieta>
                    {
                        new() { Id = 1, Refeicoes = new List<Refeicao> { new() { Id = 1, Nome = "Almoco" } } }
                    }
                },
                new Paciente { Id = 2, UsuarioId = 10, Nome = "Ana", DataCadastro = DateTime.Today },
                new Paciente { Id = 3, UsuarioId = 20, Nome = "Caio", DataCadastro = DateTime.Today.AddDays(1) });
            await context.SaveChangesAsync();

            var repository = new PacienteRepository(context);

            var pacientes = await repository.GetPacientesByUsuarioIdAsync(10);
            Assert.Equal(new[] { "Ana", "Bia" }, pacientes.Select(p => p.Nome));

            var completo = await repository.GetPacienteCompletoAsync(1, 10);
            Assert.NotNull(completo);
            Assert.Single(completo.Sessoes);
            Assert.Single(completo.Progressos);
            Assert.Single(completo.PlanosDieta.First().Refeicoes);

            var semUsuario = await repository.GetPacienteCompletoSemUsuarioAsync(1);
            Assert.NotNull(semUsuario);

            var recentes = await repository.GetPacientesRecentesAsync(10, 1);
            Assert.Single(recentes);
            Assert.Equal("Ana", recentes[0].Nome);
        }

        [Fact]
        public async Task PlanoSessaoAndProgressoRepositories_FilterByUserAndPatient()
        {
            await using var context = CreateContext();
            var pacienteUsuario = new Paciente { Id = 1, UsuarioId = 10, Nome = "Paciente A" };
            var pacienteOutro = new Paciente { Id = 2, UsuarioId = 20, Nome = "Paciente B" };
            context.Pacientes.AddRange(pacienteUsuario, pacienteOutro);
            context.PlanosDieta.AddRange(
                new PlanoDieta
                {
                    Id = 1,
                    PacienteId = 1,
                    Paciente = pacienteUsuario,
                    DataCriacao = DateTime.Today.AddDays(-1),
                    Refeicoes = new List<Refeicao> { new() { Id = 1, Nome = "Cafe" } }
                },
                new PlanoDieta { Id = 2, PacienteId = 2, Paciente = pacienteOutro, DataCriacao = DateTime.Today });
            context.Sessoes.AddRange(
                new Sessao { Id = 1, PacienteId = 1, Paciente = pacienteUsuario, DataSessao = DateTime.Today.AddDays(-1) },
                new Sessao { Id = 2, PacienteId = 1, Paciente = pacienteUsuario, DataSessao = DateTime.Today },
                new Sessao { Id = 3, PacienteId = 2, Paciente = pacienteOutro, DataSessao = DateTime.Today.AddDays(1) });
            context.Progressos.AddRange(
                new Progresso { Id = 1, PacienteId = 1, Paciente = pacienteUsuario, DataRegistro = DateTime.Today.AddDays(-1) },
                new Progresso { Id = 2, PacienteId = 1, Paciente = pacienteUsuario, DataRegistro = DateTime.Today },
                new Progresso { Id = 3, PacienteId = 2, Paciente = pacienteOutro, DataRegistro = DateTime.Today.AddDays(1) });
            await context.SaveChangesAsync();

            var planoRepository = new PlanoDietaRepository(context);
            var sessaoRepository = new SessaoRepository(context);
            var progressoRepository = new ProgressoRepository(context);

            var planos = await planoRepository.GetPlanosPorPacienteAsync(1);
            Assert.Single(planos);
            Assert.Single(planos[0].Refeicoes);
            Assert.NotNull(await planoRepository.GetPlanoCompletoAsync(1));
            Assert.Equal(1, await planoRepository.GetPlanosCountPorUsuarioAsync(10));

            var sessoes = await sessaoRepository.GetSessoesPorUsuarioAsync(10);
            Assert.Equal(new[] { 2, 1 }, sessoes.Select(s => s.Id));
            Assert.Single(await sessaoRepository.GetSessoesRecentesPorUsuarioAsync(10, 1));
            Assert.Equal(2, await sessaoRepository.GetSessoesCountPorUsuarioAsync(10));

            var progressos = await progressoRepository.GetProgressosPorPacienteAsync(1);
            Assert.Equal(new[] { 2, 1 }, progressos.Select(p => p.Id));
            Assert.Equal(2, await progressoRepository.GetProgressosCountPorUsuarioAsync(10));
        }

        [Fact]
        public async Task AuditEmbeddingAndUsuarioRepositories_FilterExpectedData()
        {
            await using var context = CreateContext();
            context.AuditLogs.AddRange(
                new AuditLog { Id = 1, UserId = "u1", PatientId = 3, Timestamp = DateTime.UtcNow.AddMinutes(-2), QueryText = "q1" },
                new AuditLog { Id = 2, UserId = "u1", PatientId = 3, Timestamp = DateTime.UtcNow, QueryText = "q2" },
                new AuditLog { Id = 3, UserId = "u2", PatientId = 3, Timestamp = DateTime.UtcNow.AddMinutes(1), QueryText = "q3" });
            context.Embeddings.AddRange(
                new EmbeddingChunk { Id = 1, PatientId = 3, ChunkText = "a", SourceTable = "Sessoes" },
                new EmbeddingChunk { Id = 2, PatientId = 4, ChunkText = "b", SourceTable = "Planos" });
            context.Usuarios.AddRange(
                new Usuario { Id = 1, Nome = "Ativo", Email = "ativo@test.com", Senha = "hash", Ativo = "S" },
                new Usuario { Id = 2, Nome = "Inativo", Email = "inativo@test.com", Senha = "hash", Ativo = "N" });
            await context.SaveChangesAsync();

            var auditRepository = new AuditLogRepository(context);
            var embeddingRepository = new EmbeddingChunkRepository(context);
            var usuarioRepository = new UsuarioRepository(context);

            var pacienteLogs = await auditRepository.GetLogsPorPacienteAsync(3, "u1", 1);
            Assert.Single(pacienteLogs);
            Assert.Equal(2, pacienteLogs[0].Id);

            Assert.Equal(2, (await auditRepository.GetLogsPorUsuarioAsync("u1", 10)).Count);
            Assert.Equal(2, (await auditRepository.GetTodosLogsPorUsuarioAsync("u1")).Count);

            Assert.Single(await embeddingRepository.GetChunksByPacienteIdAsync(3));
            await embeddingRepository.DeleteChunksByPacienteIdAsync(3);
            await context.SaveChangesAsync();
            Assert.Empty(await embeddingRepository.GetChunksByPacienteIdAsync(3));

            Assert.NotNull(await usuarioRepository.GetByEmailAsync("ativo@test.com"));
            Assert.Null(await usuarioRepository.GetByEmailAsync("inativo@test.com"));
            Assert.True(await usuarioRepository.ExisteUsuarioAsync("Ativo", "outro@test.com"));
            Assert.False(await usuarioRepository.ExisteUsuarioAsync("Novo", "novo@test.com"));
        }
    }
}
