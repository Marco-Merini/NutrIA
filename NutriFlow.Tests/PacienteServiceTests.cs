using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using NutriFlow.Models;
using NutriFlow.Services;
using NutriFlow.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace NutriFlow.Tests
{
    public class PacienteServiceTests
    {
        private readonly Mock<IPacienteRepository> _pacienteRepoMock;
        private readonly Mock<ILogger<PacienteService>> _loggerMock;
        private readonly List<Paciente> _pacientesList;

        public PacienteServiceTests()
        {
            _pacienteRepoMock = new Mock<IPacienteRepository>();
            _loggerMock = new Mock<ILogger<PacienteService>>();
            _pacientesList = new List<Paciente>();

            _pacienteRepoMock.Setup(r => r.GetPacientesByUsuarioIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int uid) => _pacientesList.Where(p => p.UsuarioId == uid).ToList());

            _pacienteRepoMock.Setup(r => r.GetPacienteCompletoAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((int id, int uid) => _pacientesList.FirstOrDefault(p => p.Id == id && p.UsuarioId == uid));

            _pacienteRepoMock.Setup(r => r.AddAsync(It.IsAny<Paciente>()))
                .Callback<Paciente>(p => _pacientesList.Add(p))
                .Returns(Task.CompletedTask);

            _pacienteRepoMock.Setup(r => r.Update(It.IsAny<Paciente>()))
                .Callback<Paciente>(p => {
                    var idx = _pacientesList.FindIndex(x => x.Id == p.Id);
                    if (idx >= 0) _pacientesList[idx] = p;
                });

            _pacienteRepoMock.Setup(r => r.Delete(It.IsAny<Paciente>()))
                .Callback<Paciente>(p => _pacientesList.Remove(p));
        }

        [Fact]
        public async Task GetPacientesByUsuarioIdAsync_ReturnsUserPatients()
        {
            // Arrange
            _pacientesList.Add(new Paciente { Id = 1, Nome = "Paciente A", UsuarioId = 10 });
            _pacientesList.Add(new Paciente { Id = 2, Nome = "Paciente B", UsuarioId = 10 });
            _pacientesList.Add(new Paciente { Id = 3, Nome = "Paciente C", UsuarioId = 20 });

            var service = new PacienteService(_pacienteRepoMock.Object, _loggerMock.Object);

            // Act
            var result = await service.GetPacientesByUsuarioIdAsync(10);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => p.Nome == "Paciente A");
        }

        [Fact]
        public async Task GetPacienteByIdAsync_ReturnsCorrectPaciente()
        {
            // Arrange
            _pacientesList.Add(new Paciente { Id = 1, Nome = "Paciente A", UsuarioId = 10 });

            var service = new PacienteService(_pacienteRepoMock.Object, _loggerMock.Object);

            // Act
            var result = await service.GetPacienteByIdAsync(1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Paciente A", result.Nome);
        }

        [Fact]
        public async Task AddPacienteAsync_SavesSuccessfully()
        {
            // Arrange
            var service = new PacienteService(_pacienteRepoMock.Object, _loggerMock.Object);
            var p = new Paciente { Id = 5, Nome = "Novo Paciente", UsuarioId = 10 };

            // Act
            var success = await service.AddPacienteAsync(p);

            // Assert
            Assert.True(success);
            Assert.Contains(_pacientesList, x => x.Nome == "Novo Paciente");
            _pacienteRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdatePacienteAsync_UpdatesSuccessfully()
        {
            // Arrange
            var p = new Paciente { Id = 1, Nome = "Nome Antigo", UsuarioId = 10 };
            _pacientesList.Add(p);

            var service = new PacienteService(_pacienteRepoMock.Object, _loggerMock.Object);
            var updated = new Paciente { Id = 1, Nome = "Nome Novo", UsuarioId = 10 };

            // Act
            var success = await service.UpdatePacienteAsync(updated);

            // Assert
            Assert.True(success);
            var saved = _pacientesList.FirstOrDefault(x => x.Id == 1);
            Assert.NotNull(saved);
            Assert.Equal("Nome Novo", saved.Nome);
            _pacienteRepoMock.Verify(r => r.Update(It.IsAny<Paciente>()), Times.Never);
            _pacienteRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeletePacienteAsync_DeletesSuccessfully()
        {
            // Arrange
            var p = new Paciente { Id = 1, Nome = "Paciente Del", UsuarioId = 10 };
            _pacientesList.Add(p);

            var service = new PacienteService(_pacienteRepoMock.Object, _loggerMock.Object);

            // Act
            var success = await service.DeletePacienteAsync(1, 10);

            // Assert
            Assert.True(success);
            Assert.Empty(_pacientesList);
        }

        [Fact]
        public async Task GetPacientesByUsuarioIdAsync_OnException_ReturnsEmptyList()
        {
            // Arrange
            _pacienteRepoMock.Setup(r => r.GetPacientesByUsuarioIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("DB Error"));
            var service = new PacienteService(_pacienteRepoMock.Object, _loggerMock.Object);

            // Act
            var result = await service.GetPacientesByUsuarioIdAsync(1);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPacienteByIdAsync_OnException_ReturnsNull()
        {
            // Arrange
            _pacienteRepoMock.Setup(r => r.GetPacienteCompletoAsync(It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new Exception("DB Error"));
            var service = new PacienteService(_pacienteRepoMock.Object, _loggerMock.Object);

            // Act
            var result = await service.GetPacienteByIdAsync(1, 1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetPacientesRecentesAsync_ReturnsRecentPatients()
        {
            // Arrange
            var recent = new List<Paciente> { new() { Id = 1, Nome = "Recent" } };
            _pacienteRepoMock.Setup(r => r.GetPacientesRecentesAsync(10, 2)).ReturnsAsync(recent);
            var service = new PacienteService(_pacienteRepoMock.Object, _loggerMock.Object);

            // Act
            var result = await service.GetPacientesRecentesAsync(10, 2);

            // Assert
            Assert.Same(recent, result);
        }

        [Fact]
        public async Task GetPacientesRecentesAsync_OnException_ReturnsEmptyList()
        {
            // Arrange
            _pacienteRepoMock.Setup(r => r.GetPacientesRecentesAsync(It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new Exception("DB Error"));
            var service = new PacienteService(_pacienteRepoMock.Object, _loggerMock.Object);

            // Act
            var result = await service.GetPacientesRecentesAsync(1, 1);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task AddPacienteAsync_OnException_ReturnsFalse()
        {
            // Arrange
            _pacienteRepoMock.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new Exception("Save failed"));
            var service = new PacienteService(_pacienteRepoMock.Object, _loggerMock.Object);

            // Act
            var result = await service.AddPacienteAsync(new Paciente());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdatePacienteAsync_WhenUsuarioIdIsNull_ReturnsFalse()
        {
            // Arrange
            var service = new PacienteService(_pacienteRepoMock.Object, _loggerMock.Object);

            // Act
            var result = await service.UpdatePacienteAsync(new Paciente { UsuarioId = null });

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdatePacienteAsync_WhenPacienteDoesNotExist_ReturnsFalse()
        {
            // Arrange
            _pacienteRepoMock.Setup(r => r.GetPacienteCompletoAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((Paciente?)null);
            var service = new PacienteService(_pacienteRepoMock.Object, _loggerMock.Object);

            // Act
            var result = await service.UpdatePacienteAsync(new Paciente { Id = 99, UsuarioId = 10 });

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdatePacienteAsync_OnException_ReturnsFalse()
        {
            // Arrange
            _pacienteRepoMock.Setup(r => r.GetPacienteCompletoAsync(It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new Exception("DB error"));
            var service = new PacienteService(_pacienteRepoMock.Object, _loggerMock.Object);

            // Act
            var result = await service.UpdatePacienteAsync(new Paciente { Id = 1, UsuarioId = 10 });

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeletePacienteAsync_WhenPacienteDoesNotExist_ReturnsFalse()
        {
            // Arrange
            _pacienteRepoMock.Setup(r => r.GetPacienteCompletoAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((Paciente?)null);
            var service = new PacienteService(_pacienteRepoMock.Object, _loggerMock.Object);

            // Act
            var result = await service.DeletePacienteAsync(99, 10);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeletePacienteAsync_OnException_ReturnsFalse()
        {
            // Arrange
            _pacienteRepoMock.Setup(r => r.GetPacienteCompletoAsync(It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new Exception("DB error"));
            var service = new PacienteService(_pacienteRepoMock.Object, _loggerMock.Object);

            // Act
            var result = await service.DeletePacienteAsync(1, 10);

            // Assert
            Assert.False(result);
        }
    }
}
