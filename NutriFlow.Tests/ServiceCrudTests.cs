using Microsoft.Extensions.Logging;
using Moq;
using NutriFlow.Models;
using NutriFlow.Repositories;
using NutriFlow.Services;
using Xunit;

namespace NutriFlow.Tests
{
    public class ServiceCrudTests
    {
        [Fact]
        public async Task SessaoService_DelegatesQueriesAndPersistsNewSessao()
        {
            var repo = new Mock<ISessaoRepository>();
            var service = new SessaoService(repo.Object, Mock.Of<ILogger<SessaoService>>());
            var sessoes = new List<Sessao> { new() { Id = 1, PacienteId = 3 } };

            repo.Setup(r => r.GetSessoesPorUsuarioAsync(7)).ReturnsAsync(sessoes);
            repo.Setup(r => r.GetSessoesRecentesPorUsuarioAsync(7, 1)).ReturnsAsync(sessoes);
            repo.Setup(r => r.GetSessoesCountPorUsuarioAsync(7)).ReturnsAsync(1);
            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(sessoes[0]);

            Assert.Same(sessoes, await service.GetSessoesPorUsuarioAsync(7));
            Assert.Same(sessoes, await service.GetSessoesRecentesPorUsuarioAsync(7, 1));
            Assert.Equal(1, await service.GetSessoesCountPorUsuarioAsync(7));
            Assert.Same(sessoes[0], await service.GetSessaoByIdAsync(1));

            var novaSessao = new Sessao { Id = 2, PacienteId = 3 };
            Assert.True(await service.AddSessaoAsync(novaSessao));
            Assert.NotNull(novaSessao.DataCriacao);
            Assert.NotNull(novaSessao.DataAtualizacao);
            repo.Verify(r => r.AddAsync(novaSessao), Times.Once);
            repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SessaoService_DeleteHandlesFoundAndMissingSessao()
        {
            var repo = new Mock<ISessaoRepository>();
            var service = new SessaoService(repo.Object, Mock.Of<ILogger<SessaoService>>());
            var sessao = new Sessao { Id = 10 };

            repo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(sessao);
            repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Sessao?)null);

            Assert.True(await service.DeleteSessaoAsync(10));
            repo.Verify(r => r.Delete(sessao), Times.Once);
            repo.Verify(r => r.SaveChangesAsync(), Times.Once);

            Assert.False(await service.DeleteSessaoAsync(99));
        }

        [Fact]
        public async Task SessaoService_ReturnsFallbacksWhenRepositoryThrows()
        {
            var repo = new Mock<ISessaoRepository>();
            var service = new SessaoService(repo.Object, Mock.Of<ILogger<SessaoService>>());

            repo.Setup(r => r.GetSessoesPorUsuarioAsync(It.IsAny<int>())).ThrowsAsync(new InvalidOperationException());
            repo.Setup(r => r.GetSessoesRecentesPorUsuarioAsync(It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new InvalidOperationException());
            repo.Setup(r => r.GetSessoesCountPorUsuarioAsync(It.IsAny<int>())).ThrowsAsync(new InvalidOperationException());
            repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new InvalidOperationException());
            repo.Setup(r => r.AddAsync(It.IsAny<Sessao>())).ThrowsAsync(new InvalidOperationException());

            Assert.Empty(await service.GetSessoesPorUsuarioAsync(1));
            Assert.Empty(await service.GetSessoesRecentesPorUsuarioAsync(1, 2));
            Assert.Equal(0, await service.GetSessoesCountPorUsuarioAsync(1));
            Assert.Null(await service.GetSessaoByIdAsync(1));
            Assert.False(await service.AddSessaoAsync(new Sessao()));
            Assert.False(await service.DeleteSessaoAsync(1));
        }

        [Fact]
        public async Task PlanoDietaService_DelegatesQueriesAndPersistsRefeicoes()
        {
            var repo = new Mock<IPlanoDietaRepository>();
            var service = new PlanoDietaService(repo.Object, Mock.Of<ILogger<PlanoDietaService>>());
            var planos = new List<PlanoDieta> { new() { Id = 1, PacienteId = 4 } };

            repo.Setup(r => r.GetPlanosPorPacienteAsync(4)).ReturnsAsync(planos);
            repo.Setup(r => r.GetPlanoCompletoAsync(1)).ReturnsAsync(planos[0]);
            repo.Setup(r => r.GetPlanosCountPorUsuarioAsync(8)).ReturnsAsync(1);

            Assert.Same(planos, await service.GetPlanosPorPacienteAsync(4));
            Assert.Same(planos[0], await service.GetPlanoCompletoAsync(1));
            Assert.Equal(1, await service.GetPlanosCountPorUsuarioAsync(8));

            var plano = new PlanoDieta
            {
                Id = 2,
                PacienteId = 4,
                Refeicoes = new List<Refeicao> { new() { Id = 5, Nome = "Cafe" } }
            };

            Assert.True(await service.AddPlanoDietaAsync(plano));
            Assert.NotNull(plano.DataCriacao);
            Assert.NotNull(plano.DataAtualizacao);
            Assert.NotNull(plano.Refeicoes.First().DataCriacao);
            repo.Verify(r => r.AddAsync(plano), Times.Once);
            repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task PlanoDietaService_DeleteHandlesFoundAndMissingPlano()
        {
            var repo = new Mock<IPlanoDietaRepository>();
            var service = new PlanoDietaService(repo.Object, Mock.Of<ILogger<PlanoDietaService>>());
            var plano = new PlanoDieta { Id = 11 };

            repo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(plano);
            repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((PlanoDieta?)null);

            Assert.True(await service.DeletePlanoDietaAsync(11));
            repo.Verify(r => r.Delete(plano), Times.Once);
            repo.Verify(r => r.SaveChangesAsync(), Times.Once);

            Assert.False(await service.DeletePlanoDietaAsync(99));
        }

        [Fact]
        public async Task PlanoDietaService_ReturnsFallbacksWhenRepositoryThrows()
        {
            var repo = new Mock<IPlanoDietaRepository>();
            var service = new PlanoDietaService(repo.Object, Mock.Of<ILogger<PlanoDietaService>>());

            repo.Setup(r => r.GetPlanosPorPacienteAsync(It.IsAny<int>())).ThrowsAsync(new InvalidOperationException());
            repo.Setup(r => r.GetPlanoCompletoAsync(It.IsAny<int>())).ThrowsAsync(new InvalidOperationException());
            repo.Setup(r => r.GetPlanosCountPorUsuarioAsync(It.IsAny<int>())).ThrowsAsync(new InvalidOperationException());
            repo.Setup(r => r.AddAsync(It.IsAny<PlanoDieta>())).ThrowsAsync(new InvalidOperationException());
            repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new InvalidOperationException());

            Assert.Empty(await service.GetPlanosPorPacienteAsync(1));
            Assert.Null(await service.GetPlanoCompletoAsync(1));
            Assert.Equal(0, await service.GetPlanosCountPorUsuarioAsync(1));
            Assert.False(await service.AddPlanoDietaAsync(new PlanoDieta()));
            Assert.False(await service.DeletePlanoDietaAsync(1));
        }

        [Fact]
        public async Task ProgressoService_DelegatesQueriesAndPersistsNewProgresso()
        {
            var repo = new Mock<IProgressoRepository>();
            var service = new ProgressoService(repo.Object, Mock.Of<ILogger<ProgressoService>>());
            var progressos = new List<Progresso> { new() { Id = 1, PacienteId = 4 } };

            repo.Setup(r => r.GetProgressosPorPacienteAsync(4)).ReturnsAsync(progressos);
            repo.Setup(r => r.GetProgressosCountPorUsuarioAsync(8)).ReturnsAsync(1);
            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(progressos[0]);

            Assert.Same(progressos, await service.GetProgressosPorPacienteAsync(4));
            Assert.Equal(1, await service.GetProgressosCountPorUsuarioAsync(8));
            Assert.Same(progressos[0], await service.GetProgressoByIdAsync(1));

            var progresso = new Progresso { Id = 2, PacienteId = 4 };
            Assert.True(await service.AddProgressoAsync(progresso));
            Assert.NotNull(progresso.DataCriacao);
            Assert.NotNull(progresso.DataAtualizacao);
            repo.Verify(r => r.AddAsync(progresso), Times.Once);
            repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ProgressoService_DeleteHandlesFoundAndMissingProgresso()
        {
            var repo = new Mock<IProgressoRepository>();
            var service = new ProgressoService(repo.Object, Mock.Of<ILogger<ProgressoService>>());
            var progresso = new Progresso { Id = 12 };

            repo.Setup(r => r.GetByIdAsync(12)).ReturnsAsync(progresso);
            repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Progresso?)null);

            Assert.True(await service.DeleteProgressoAsync(12));
            repo.Verify(r => r.Delete(progresso), Times.Once);
            repo.Verify(r => r.SaveChangesAsync(), Times.Once);

            Assert.False(await service.DeleteProgressoAsync(99));
        }

        [Fact]
        public async Task ProgressoService_ReturnsFallbacksWhenRepositoryThrows()
        {
            var repo = new Mock<IProgressoRepository>();
            var service = new ProgressoService(repo.Object, Mock.Of<ILogger<ProgressoService>>());

            repo.Setup(r => r.GetProgressosPorPacienteAsync(It.IsAny<int>())).ThrowsAsync(new InvalidOperationException());
            repo.Setup(r => r.GetProgressosCountPorUsuarioAsync(It.IsAny<int>())).ThrowsAsync(new InvalidOperationException());
            repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new InvalidOperationException());
            repo.Setup(r => r.AddAsync(It.IsAny<Progresso>())).ThrowsAsync(new InvalidOperationException());

            Assert.Empty(await service.GetProgressosPorPacienteAsync(1));
            Assert.Equal(0, await service.GetProgressosCountPorUsuarioAsync(1));
            Assert.Null(await service.GetProgressoByIdAsync(1));
            Assert.False(await service.AddProgressoAsync(new Progresso()));
            Assert.False(await service.DeleteProgressoAsync(1));
        }
    }
}
