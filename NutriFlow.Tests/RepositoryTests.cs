using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NutriFlow.Data;
using NutriFlow.Models;
using NutriFlow.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NutriFlow.Tests
{
    public class RepositoryTests
    {
        private static (ApplicationDbContext context, SqliteConnection connection) CreateSQLiteContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();

            return (context, connection);
        }

        [Fact]
        public async Task BaseRepository_AddsUpdatesDeletesAndReadsEntities()
        {
            var (context, connection) = CreateSQLiteContext();
            try
            {
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
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task PacienteRepository_ReturnsOnlyUserPatientsAndIncludesRelations()
        {
            var (context, connection) = CreateSQLiteContext();
            try
            {
                context.Usuarios.AddRange(
                    new Usuario { Id = 10, Nome = "Nutri 10", Email = "n10@test.com", Senha = "hash", Ativo = "S" },
                    new Usuario { Id = 20, Nome = "Nutri 20", Email = "n20@test.com", Senha = "hash", Ativo = "S" }
                );
                await context.SaveChangesAsync();

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
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task PlanoSessaoAndProgressoRepositories_FilterByUserAndPatient()
        {
            var (context, connection) = CreateSQLiteContext();
            try
            {
                context.Usuarios.AddRange(
                    new Usuario { Id = 10, Nome = "Nutri 10", Email = "n10@test.com", Senha = "hash", Ativo = "S" },
                    new Usuario { Id = 20, Nome = "Nutri 20", Email = "n20@test.com", Senha = "hash", Ativo = "S" }
                );
                await context.SaveChangesAsync();

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
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task AuditEmbeddingAndUsuarioRepositories_FilterExpectedData()
        {
            var (context, connection) = CreateSQLiteContext();
            try
            {
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
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task PlanoDietaRepository_GetPlanosFiltradosAsync_AppliesAllFiltersCorrectly()
        {
            var (context, connection) = CreateSQLiteContext();
            try
            {
                context.Usuarios.AddRange(
                    new Usuario { Id = 10, Nome = "Nutri 10", Email = "n10@test.com", Senha = "hash", Ativo = "S" },
                    new Usuario { Id = 99, Nome = "Nutri 99", Email = "n99@test.com", Senha = "hash", Ativo = "S" }
                );
                await context.SaveChangesAsync();

                var paciente1 = new Paciente { Id = 1, UsuarioId = 10, Nome = "Paciente 1" };
                var paciente2 = new Paciente { Id = 2, UsuarioId = 10, Nome = "Paciente 2" };
                var pacienteOutro = new Paciente { Id = 9, UsuarioId = 99, Nome = "Paciente Outro" };
                context.Pacientes.AddRange(paciente1, paciente2, pacienteOutro);

                context.PlanosDieta.AddRange(
                    new PlanoDieta { Id = 1, PacienteId = 1, Paciente = paciente1, Titulo = "Plano Especial A", DataCriacao = new DateTime(2026, 6, 1), CaloriasDiarias = 1500M },
                    new PlanoDieta { Id = 2, PacienteId = 1, Paciente = paciente1, Titulo = "Dieta B", DataCriacao = new DateTime(2026, 6, 5), CaloriasDiarias = 2200M },
                    new PlanoDieta { Id = 3, PacienteId = 2, Paciente = paciente2, Titulo = "Plano Especial C", DataCriacao = new DateTime(2026, 6, 10), CaloriasDiarias = 1800M },
                    new PlanoDieta { Id = 99, PacienteId = 9, Paciente = pacienteOutro, Titulo = "Plano Secreto", DataCriacao = new DateTime(2026, 6, 1) }
                );
                await context.SaveChangesAsync();

                var repository = new PlanoDietaRepository(context);

                // 1. Filtrar por Nome (contendo "Especial") + Multi-tenant isolation (não deve trazer o plano de UsuarioId 99)
                var (items, count) = await repository.GetPlanosFiltradosAsync(10, new PlanoDietaFilter { Nome = "Especial", Page = 1, PageSize = 10 });
                Assert.Equal(2, count);
                Assert.Contains(items, p => p.Titulo == "Plano Especial A");
                Assert.Contains(items, p => p.Titulo == "Plano Especial C");

                // 2. Filtrar por PacienteId
                var (items2, count2) = await repository.GetPlanosFiltradosAsync(10, new PlanoDietaFilter { PacienteId = 1, Page = 1, PageSize = 10 });
                Assert.Equal(2, count2);
                Assert.All(items2, p => Assert.Equal(1, p.PacienteId));

                // 3. Filtrar por Datas (entre 2026-06-03 e 2026-06-07)
                var (items3, count3) = await repository.GetPlanosFiltradosAsync(10, new PlanoDietaFilter 
                { 
                    DataInicio = new DateTime(2026, 6, 3), 
                    DataFim = new DateTime(2026, 6, 7), 
                    Page = 1, 
                    PageSize = 10 
                });
                Assert.Equal(1, count3);
                Assert.Equal("Dieta B", items3.First().Titulo);

                // 4. Paginação e Ordenação Ascendente por Titulo
                var (items4, count4) = await repository.GetPlanosFiltradosAsync(10, new PlanoDietaFilter 
                { 
                    Page = 1, 
                    PageSize = 10, 
                    SortBy = "titulo", 
                    SortDirection = "asc" 
                });
                Assert.Equal(3, count4);
                Assert.Equal("Dieta B", items4[0].Titulo); // D, P, P
                Assert.Equal("Plano Especial A", items4[1].Titulo);
                Assert.Equal("Plano Especial C", items4[2].Titulo);

                // 5. Ordenação Descendente por CaloriasDiarias
                var (items5, count5) = await repository.GetPlanosFiltradosAsync(10, new PlanoDietaFilter 
                { 
                    Page = 1, 
                    PageSize = 10, 
                    SortBy = "caloriasdiarias", 
                    SortDirection = "desc" 
                });
                Assert.Equal(3, count5);
                Assert.Equal(2200M, items5[0].CaloriasDiarias); // 2200, 1800, 1500
                Assert.Equal(1800M, items5[1].CaloriasDiarias);
                Assert.Equal(1500M, items5[2].CaloriasDiarias);
            }
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task ProgressoRepository_GetProgressosFiltradosAsync_AppliesAllFiltersCorrectly()
        {
            var (context, connection) = CreateSQLiteContext();
            try
            {
                context.Usuarios.AddRange(
                    new Usuario { Id = 10, Nome = "Nutri 10", Email = "n10@test.com", Senha = "hash", Ativo = "S" },
                    new Usuario { Id = 99, Nome = "Nutri 99", Email = "n99@test.com", Senha = "hash", Ativo = "S" }
                );
                await context.SaveChangesAsync();

                var paciente1 = new Paciente { Id = 1, UsuarioId = 10, Nome = "Paciente 1" };
                var paciente2 = new Paciente { Id = 2, UsuarioId = 10, Nome = "Paciente 2" };
                var pacienteOutro = new Paciente { Id = 9, UsuarioId = 99, Nome = "Paciente Outro" };
                context.Pacientes.AddRange(paciente1, paciente2, pacienteOutro);

                context.Progressos.AddRange(
                    new Progresso { Id = 1, PacienteId = 1, Paciente = paciente1, Peso = 70.5m, DataRegistro = new DateTime(2026, 6, 1), DataCriacao = new DateTime(2026, 6, 1) },
                    new Progresso { Id = 2, PacienteId = 1, Paciente = paciente1, Peso = 69.2m, DataRegistro = new DateTime(2026, 6, 5), DataCriacao = new DateTime(2026, 6, 5) },
                    new Progresso { Id = 3, PacienteId = 2, Paciente = paciente2, Peso = 85.0m, DataRegistro = new DateTime(2026, 6, 10), DataCriacao = new DateTime(2026, 6, 10) },
                    new Progresso { Id = 99, PacienteId = 9, Paciente = pacienteOutro, Peso = 90m, DataRegistro = new DateTime(2026, 6, 1), DataCriacao = new DateTime(2026, 6, 1) }
                );
                await context.SaveChangesAsync();

                var repository = new ProgressoRepository(context);

                // 1. Filtrar por PacienteId + Multi-tenant isolation
                var (items, count) = await repository.GetProgressosFiltradosAsync(10, new ProgressoFilter { PacienteId = 1, Page = 1, PageSize = 10 });
                Assert.Equal(2, count);
                Assert.All(items, p => Assert.Equal(1, p.PacienteId));

                // 2. Filtrar por Datas
                var (items2, count2) = await repository.GetProgressosFiltradosAsync(10, new ProgressoFilter 
                { 
                    DataInicio = new DateTime(2026, 6, 3), 
                    DataFim = new DateTime(2026, 6, 7), 
                    Page = 1, 
                    PageSize = 10 
                });
                Assert.Equal(1, count2);
                Assert.Equal(69.2m, items2.First().Peso);

                // 3. Ordenação por DataRegistro Ascendente
                var (items3, count3) = await repository.GetProgressosFiltradosAsync(10, new ProgressoFilter 
                { 
                    Page = 1, 
                    PageSize = 10, 
                    SortBy = "dataregistro", 
                    SortDirection = "asc" 
                });
                Assert.Equal(3, count3);
                Assert.Equal(70.5m, items3[0].Peso); // 1, 5, 10
                Assert.Equal(69.2m, items3[1].Peso);
                Assert.Equal(85.0m, items3[2].Peso);
            }
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task SessaoRepository_GetSessoesFiltradasAsync_AppliesAllFiltersCorrectly()
        {
            var (context, connection) = CreateSQLiteContext();
            try
            {
                context.Usuarios.AddRange(
                    new Usuario { Id = 10, Nome = "Nutri 10", Email = "n10@test.com", Senha = "hash", Ativo = "S" },
                    new Usuario { Id = 99, Nome = "Nutri 99", Email = "n99@test.com", Senha = "hash", Ativo = "S" }
                );
                await context.SaveChangesAsync();

                var paciente1 = new Paciente { Id = 1, UsuarioId = 10, Nome = "Paciente 1" };
                var paciente2 = new Paciente { Id = 2, UsuarioId = 10, Nome = "Paciente 2" };
                var pacienteOutro = new Paciente { Id = 9, UsuarioId = 99, Nome = "Paciente Outro" };
                context.Pacientes.AddRange(paciente1, paciente2, pacienteOutro);

                context.Sessoes.AddRange(
                    new Sessao { Id = 1, PacienteId = 1, Paciente = paciente1, Tipo = "Avaliação", DataSessao = new DateTime(2026, 6, 1), DataCriacao = new DateTime(2026, 6, 1) },
                    new Sessao { Id = 2, PacienteId = 1, Paciente = paciente1, Tipo = "Retorno", DataSessao = new DateTime(2026, 6, 5), DataCriacao = new DateTime(2026, 6, 5) },
                    new Sessao { Id = 3, PacienteId = 2, Paciente = paciente2, Tipo = "Avaliação", DataSessao = new DateTime(2026, 6, 10), DataCriacao = new DateTime(2026, 6, 10) },
                    new Sessao { Id = 99, PacienteId = 9, Paciente = pacienteOutro, Tipo = "Consulta Secreta", DataSessao = new DateTime(2026, 6, 1), DataCriacao = new DateTime(2026, 6, 1) }
                );
                await context.SaveChangesAsync();

                var repository = new SessaoRepository(context);

                // 1. Filtrar por PacienteId + Multi-tenant isolation
                var (items, count) = await repository.GetSessoesFiltradasAsync(10, new SessaoFilter { PacienteId = 1, Page = 1, PageSize = 10 });
                Assert.Equal(2, count);
                Assert.All(items, s => Assert.Equal(1, s.PacienteId));

                // 2. Filtrar por Datas
                var (items2, count2) = await repository.GetSessoesFiltradasAsync(10, new SessaoFilter 
                { 
                    DataInicio = new DateTime(2026, 6, 3), 
                    DataFim = new DateTime(2026, 6, 7), 
                    Page = 1, 
                    PageSize = 10 
                });
                Assert.Equal(1, count2);
                Assert.Equal("Retorno", items2.First().Tipo);

                // 3. Ordenação por DataSessao Ascendente
                var (items3, count3) = await repository.GetSessoesFiltradasAsync(10, new SessaoFilter 
                { 
                    Page = 1, 
                    PageSize = 10, 
                    SortBy = "datasessao", 
                    SortDirection = "asc" 
                });
                Assert.Equal(3, count3);
                Assert.Equal("Avaliação", items3[0].Tipo); // 1, 5, 10
                Assert.Equal("Retorno", items3[1].Tipo);
            }
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }
    }
}
