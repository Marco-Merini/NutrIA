using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using NutriFlow.Components.Pages;
using NutriFlow.Models;
using NutriFlow.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace NutriFlow.Tests
{
    public class BlazorPageTests : BunitContext, IAsyncLifetime
    {
        public BlazorPageTests()
        {
            // Set bUnit JSInterop to Loose mode to automatically mock MudBlazor JS calls
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddMudServices(options =>
            {
                options.PopoverOptions.CheckForPopoverProvider = false;
            });

            // Mock Authorization
            var authContext = this.AddAuthorization();
            authContext.SetAuthorized("Nutri");
            authContext.SetClaims(new Claim("UsuarioId", "10"));
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public new async Task DisposeAsync()
        {
            await base.DisposeAsync();
        }

        [Fact]
        public void PlanosAlimentares_RendersAndTriggersSearch()
        {
            // Arrange
            var planoDietaServiceMock = new Mock<IPlanoDietaService>();
            var pacienteServiceMock = new Mock<IPacienteService>();
            var authServiceMock = new Mock<IAuthService>();

            var currentUser = new Usuario { Id = 10, Nome = "Nutri" };
            authServiceMock.SetupGet(a => a.CurrentUser).Returns(currentUser);

            var pacientes = new List<Paciente> { new() { Id = 1, Nome = "Lucas" } };
            pacienteServiceMock.Setup(p => p.GetPacientesByUsuarioIdAsync(10))
                .ReturnsAsync(pacientes);

            var expectedResult = new PaginatedResult<PlanoDietaResponseDto>(
                new List<PlanoDietaResponseDto> { new() { Id = 1, Titulo = "Plano Especial", PacienteNome = "Lucas", CaloriasDiarias = 2000M } },
                1, 1, 10
            );

            planoDietaServiceMock.Setup(s => s.GetPlanosFiltradosAsync(10, It.IsAny<PlanoDietaFilter>()))
                .ReturnsAsync(expectedResult);

            Services.AddSingleton(planoDietaServiceMock.Object);
            Services.AddSingleton(pacienteServiceMock.Object);
            Services.AddSingleton(authServiceMock.Object);
            Services.AddSingleton(Mock.Of<ISnackbar>());
            Services.AddSingleton(Mock.Of<IDialogService>());

            // Act
            var cut = Render<PlanosAlimentares>();

            // Assert
            // 1. Verify that the search layout is rendered
            Assert.Contains("Planos Alimentares", cut.Markup);
            Assert.Contains("Buscar", cut.Markup);

            // 2. Verify that the filter service was called on render
            planoDietaServiceMock.Verify(s => s.GetPlanosFiltradosAsync(10, It.Is<PlanoDietaFilter>(f => f.Page == 1)), Times.AtLeastOnce);

            // 3. Find and click the search button
            var searchButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Buscar"));
            Assert.NotNull(searchButton);
            searchButton.Click();

            // 4. Verify service called again on manual click
            planoDietaServiceMock.Verify(s => s.GetPlanosFiltradosAsync(10, It.IsAny<PlanoDietaFilter>()), Times.AtLeast(2));
        }

        [Fact]
        public void Progresso_RendersAndTriggersSearch()
        {
            // Arrange
            var progressoServiceMock = new Mock<IProgressoService>();
            var pacienteServiceMock = new Mock<IPacienteService>();
            var authServiceMock = new Mock<IAuthService>();

            var currentUser = new Usuario { Id = 10, Nome = "Nutri" };
            authServiceMock.SetupGet(a => a.CurrentUser).Returns(currentUser);

            var pacientes = new List<Paciente> { new() { Id = 1, Nome = "Lucas" } };
            pacienteServiceMock.Setup(p => p.GetPacientesByUsuarioIdAsync(10))
                .ReturnsAsync(pacientes);

            var expectedResult = new PaginatedResult<ProgressoResponseDto>(
                new List<ProgressoResponseDto> { new() { Id = 1, PacienteNome = "Lucas", Peso = 75.5m } },
                1, 1, 10
            );

            progressoServiceMock.Setup(s => s.GetProgressosFiltradosAsync(10, It.IsAny<ProgressoFilter>()))
                .ReturnsAsync(expectedResult);

            Services.AddSingleton(progressoServiceMock.Object);
            Services.AddSingleton(pacienteServiceMock.Object);
            Services.AddSingleton(authServiceMock.Object);
            Services.AddSingleton(Mock.Of<ISnackbar>());
            Services.AddSingleton(Mock.Of<IDialogService>());

            // Act
            var cut = Render<NutriFlow.Components.Pages.Progresso>();

            // Assert
            Assert.Contains("Monitoramento de Progresso", cut.Markup);
            Assert.Contains("Buscar", cut.Markup);

            progressoServiceMock.Verify(s => s.GetProgressosFiltradosAsync(10, It.Is<ProgressoFilter>(f => f.Page == 1)), Times.AtLeastOnce);

            var searchButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Buscar"));
            Assert.NotNull(searchButton);
            searchButton.Click();

            progressoServiceMock.Verify(s => s.GetProgressosFiltradosAsync(10, It.IsAny<ProgressoFilter>()), Times.AtLeast(2));
        }

        [Fact]
        public void Sessoes_RendersAndTriggersSearch()
        {
            // Arrange
            var sessaoServiceMock = new Mock<ISessaoService>();
            var pacienteServiceMock = new Mock<IPacienteService>();
            var authServiceMock = new Mock<IAuthService>();

            var currentUser = new Usuario { Id = 10, Nome = "Nutri" };
            authServiceMock.SetupGet(a => a.CurrentUser).Returns(currentUser);

            var pacientes = new List<Paciente> { new() { Id = 1, Nome = "Lucas" } };
            pacienteServiceMock.Setup(p => p.GetPacientesByUsuarioIdAsync(10))
                .ReturnsAsync(pacientes);

            var expectedResult = new PaginatedResult<SessaoResponseDto>(
                new List<SessaoResponseDto> { new() { Id = 1, PacienteNome = "Lucas", Tipo = "Avaliação" } },
                1, 1, 10
            );

            sessaoServiceMock.Setup(s => s.GetSessoesFiltradasAsync(10, It.IsAny<SessaoFilter>()))
                .ReturnsAsync(expectedResult);

            Services.AddSingleton(sessaoServiceMock.Object);
            Services.AddSingleton(pacienteServiceMock.Object);
            Services.AddSingleton(authServiceMock.Object);
            Services.AddSingleton(Mock.Of<ISnackbar>());
            Services.AddSingleton(Mock.Of<IDialogService>());

            // Act
            var cut = Render<Sessoes>();

            // Assert
            Assert.Contains("Sessões", cut.Markup);
            Assert.Contains("Buscar", cut.Markup);

            sessaoServiceMock.Verify(s => s.GetSessoesFiltradasAsync(10, It.Is<SessaoFilter>(f => f.Page == 1)), Times.AtLeastOnce);

            var searchButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Buscar"));
            Assert.NotNull(searchButton);
            searchButton.Click();

            sessaoServiceMock.Verify(s => s.GetSessoesFiltradasAsync(10, It.IsAny<SessaoFilter>()), Times.AtLeast(2));
        }

        [Fact]
        public void PlanosAlimentares_ResetsFilters()
        {
            // Arrange
            var planoDietaServiceMock = new Mock<IPlanoDietaService>();
            var pacienteServiceMock = new Mock<IPacienteService>();
            var authServiceMock = new Mock<IAuthService>();

            var currentUser = new Usuario { Id = 10, Nome = "Nutri" };
            authServiceMock.SetupGet(a => a.CurrentUser).Returns(currentUser);

            var pacientes = new List<Paciente>();
            pacienteServiceMock.Setup(p => p.GetPacientesByUsuarioIdAsync(10)).ReturnsAsync(pacientes);

            var expectedResult = new PaginatedResult<PlanoDietaResponseDto>(new List<PlanoDietaResponseDto>(), 0, 1, 10);
            planoDietaServiceMock.Setup(s => s.GetPlanosFiltradosAsync(10, It.IsAny<PlanoDietaFilter>())).ReturnsAsync(expectedResult);

            Services.AddSingleton(planoDietaServiceMock.Object);
            Services.AddSingleton(pacienteServiceMock.Object);
            Services.AddSingleton(authServiceMock.Object);
            Services.AddSingleton(Mock.Of<ISnackbar>());
            Services.AddSingleton(Mock.Of<IDialogService>());

            var cut = Render<PlanosAlimentares>();

            // Act - Find and click the reset button
            var resetButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Limpar"));
            Assert.NotNull(resetButton);
            resetButton.Click();

            // Assert
            planoDietaServiceMock.Verify(s => s.GetPlanosFiltradosAsync(10, It.Is<PlanoDietaFilter>(f => f.Page == 1 && f.Nome == null)), Times.AtLeastOnce);
        }

        [Fact]
        public void Progresso_ResetsFilters()
        {
            // Arrange
            var progressoServiceMock = new Mock<IProgressoService>();
            var pacienteServiceMock = new Mock<IPacienteService>();
            var authServiceMock = new Mock<IAuthService>();

            var currentUser = new Usuario { Id = 10, Nome = "Nutri" };
            authServiceMock.SetupGet(a => a.CurrentUser).Returns(currentUser);

            var pacientes = new List<Paciente>();
            pacienteServiceMock.Setup(p => p.GetPacientesByUsuarioIdAsync(10)).ReturnsAsync(pacientes);

            var expectedResult = new PaginatedResult<ProgressoResponseDto>(new List<ProgressoResponseDto>(), 0, 1, 10);
            progressoServiceMock.Setup(s => s.GetProgressosFiltradosAsync(10, It.IsAny<ProgressoFilter>())).ReturnsAsync(expectedResult);

            Services.AddSingleton(progressoServiceMock.Object);
            Services.AddSingleton(pacienteServiceMock.Object);
            Services.AddSingleton(authServiceMock.Object);
            Services.AddSingleton(Mock.Of<ISnackbar>());
            Services.AddSingleton(Mock.Of<IDialogService>());

            var cut = Render<NutriFlow.Components.Pages.Progresso>();

            // Act
            var resetButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Limpar"));
            Assert.NotNull(resetButton);
            resetButton.Click();

            // Assert
            progressoServiceMock.Verify(s => s.GetProgressosFiltradosAsync(10, It.Is<ProgressoFilter>(f => f.Page == 1 && f.PacienteId == null)), Times.AtLeastOnce);
        }

        [Fact]
        public void Sessoes_ResetsFilters()
        {
            // Arrange
            var sessaoServiceMock = new Mock<ISessaoService>();
            var pacienteServiceMock = new Mock<IPacienteService>();
            var authServiceMock = new Mock<IAuthService>();

            var currentUser = new Usuario { Id = 10, Nome = "Nutri" };
            authServiceMock.SetupGet(a => a.CurrentUser).Returns(currentUser);

            var pacientes = new List<Paciente>();
            pacienteServiceMock.Setup(p => p.GetPacientesByUsuarioIdAsync(10)).ReturnsAsync(pacientes);

            var expectedResult = new PaginatedResult<SessaoResponseDto>(new List<SessaoResponseDto>(), 0, 1, 10);
            sessaoServiceMock.Setup(s => s.GetSessoesFiltradasAsync(10, It.IsAny<SessaoFilter>())).ReturnsAsync(expectedResult);

            Services.AddSingleton(sessaoServiceMock.Object);
            Services.AddSingleton(pacienteServiceMock.Object);
            Services.AddSingleton(authServiceMock.Object);
            Services.AddSingleton(Mock.Of<ISnackbar>());
            Services.AddSingleton(Mock.Of<IDialogService>());

            var cut = Render<Sessoes>();

            // Act
            var resetButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Limpar"));
            Assert.NotNull(resetButton);
            resetButton.Click();

            // Assert
            sessaoServiceMock.Verify(s => s.GetSessoesFiltradasAsync(10, It.Is<SessaoFilter>(f => f.Page == 1 && f.PacienteId == null)), Times.AtLeastOnce);
        }
    }
}
