using Xunit;
using NutriFlow.Services;
using System.Collections.Generic;

namespace NutriFlow.Tests
{
    public class NavigationServiceTests
    {
        [Fact]
        public void NavigationService_TriggersCorrectPageChangedEvents()
        {
            // Arrange
            var service = new NavigationService();
            var events = new List<string>();
            service.PageChanged += (path) => events.Add(path);

            // Act & Assert
            service.NavigateToDashboard();
            Assert.Single(events);
            Assert.Equal("/", events[^1]);

            service.NavigateToPacientes();
            Assert.Equal(2, events.Count);
            Assert.Equal("/pacientes", events[^1]);

            service.NavigateToPacienteDetalhes(42);
            Assert.Equal(3, events.Count);
            Assert.Equal("/pacientes/42", events[^1]);

            service.NavigateToDietas();
            Assert.Equal(4, events.Count);
            Assert.Equal("/dietas", events[^1]);

            service.NavigateToPlanoDetalhes(99);
            Assert.Equal(5, events.Count);
            Assert.Equal("/dietas/99", events[^1]);

            service.NavigateToSessoes();
            Assert.Equal(6, events.Count);
            Assert.Equal("/sessoes", events[^1]);

            service.NavigateToProgresso();
            Assert.Equal(7, events.Count);
            Assert.Equal("/progresso", events[^1]);

            service.NavigateToAssistenteIA();
            Assert.Equal(8, events.Count);
            Assert.Equal("/assistente-ia", events[^1]);
        }
    }
}
