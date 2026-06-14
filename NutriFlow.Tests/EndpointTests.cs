using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NutriFlow.Endpoints;
using NutriFlow.Models;
using NutriFlow.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace NutriFlow.Tests
{
    public class EndpointTests
    {
        private HttpContext CreateHttpContextWithUser(string? usuarioId)
        {
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>();
            if (usuarioId != null)
            {
                claims.Add(new Claim("UsuarioId", usuarioId));
            }
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
            return httpContext;
        }

        [Fact]
        public async Task PlanosEndpoints_GetPlanosAsync_ReturnsOkWithData_WhenAuthenticated()
        {
            // Arrange
            var httpContext = CreateHttpContextWithUser("10");
            var filter = new PlanoDietaFilter { Page = 1, PageSize = 10, Nome = "Dieta" };
            var serviceMock = new Mock<IPlanoDietaService>();
            var expectedResult = new PaginatedResult<PlanoDietaResponseDto>(
                new List<PlanoDietaResponseDto> { new() { Id = 1, Titulo = "Plano A" } },
                1, 1, 10
            );

            serviceMock.Setup(s => s.GetPlanosFiltradosAsync(10, filter))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await PlanosEndpoints.GetPlanosAsync(httpContext, filter, serviceMock.Object);

            // Assert
            var okResult = Assert.IsType<Ok<PaginatedResult<PlanoDietaResponseDto>>>(result);
            Assert.Same(expectedResult, okResult.Value);
            serviceMock.Verify(s => s.GetPlanosFiltradosAsync(10, filter), Times.Once);
        }

        [Fact]
        public async Task PlanosEndpoints_GetPlanosAsync_ReturnsUnauthorized_WhenNoClaim()
        {
            // Arrange
            var httpContext = CreateHttpContextWithUser(null);
            var filter = new PlanoDietaFilter();
            var serviceMock = new Mock<IPlanoDietaService>();

            // Act
            var result = await PlanosEndpoints.GetPlanosAsync(httpContext, filter, serviceMock.Object);

            // Assert
            Assert.IsType<UnauthorizedHttpResult>(result);
            serviceMock.Verify(s => s.GetPlanosFiltradosAsync(It.IsAny<int>(), It.IsAny<PlanoDietaFilter>()), Times.Never);
        }

        [Fact]
        public async Task PlanosEndpoints_GetPlanosAsync_ReturnsUnauthorized_WhenClaimInvalid()
        {
            // Arrange
            var httpContext = CreateHttpContextWithUser("invalid_id");
            var filter = new PlanoDietaFilter();
            var serviceMock = new Mock<IPlanoDietaService>();

            // Act
            var result = await PlanosEndpoints.GetPlanosAsync(httpContext, filter, serviceMock.Object);

            // Assert
            Assert.IsType<UnauthorizedHttpResult>(result);
            serviceMock.Verify(s => s.GetPlanosFiltradosAsync(It.IsAny<int>(), It.IsAny<PlanoDietaFilter>()), Times.Never);
        }

        [Fact]
        public async Task ProgressoEndpoints_GetProgressosAsync_ReturnsOkWithData_WhenAuthenticated()
        {
            // Arrange
            var httpContext = CreateHttpContextWithUser("20");
            var filter = new ProgressoFilter { Page = 1, PageSize = 5 };
            var serviceMock = new Mock<IProgressoService>();
            var expectedResult = new PaginatedResult<ProgressoResponseDto>(
                new List<ProgressoResponseDto> { new() { Id = 2, Peso = 75.5m } },
                1, 1, 5
            );

            serviceMock.Setup(s => s.GetProgressosFiltradosAsync(20, filter))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await ProgressoEndpoints.GetProgressosAsync(httpContext, filter, serviceMock.Object);

            // Assert
            var okResult = Assert.IsType<Ok<PaginatedResult<ProgressoResponseDto>>>(result);
            Assert.Same(expectedResult, okResult.Value);
            serviceMock.Verify(s => s.GetProgressosFiltradosAsync(20, filter), Times.Once);
        }

        [Fact]
        public async Task ProgressoEndpoints_GetProgressosAsync_ReturnsUnauthorized_WhenNoClaim()
        {
            // Arrange
            var httpContext = CreateHttpContextWithUser(null);
            var filter = new ProgressoFilter();
            var serviceMock = new Mock<IProgressoService>();

            // Act
            var result = await ProgressoEndpoints.GetProgressosAsync(httpContext, filter, serviceMock.Object);

            // Assert
            Assert.IsType<UnauthorizedHttpResult>(result);
            serviceMock.Verify(s => s.GetProgressosFiltradosAsync(It.IsAny<int>(), It.IsAny<ProgressoFilter>()), Times.Never);
        }

        [Fact]
        public async Task SessoesEndpoints_GetSessoesAsync_ReturnsOkWithData_WhenAuthenticated()
        {
            // Arrange
            var httpContext = CreateHttpContextWithUser("30");
            var filter = new SessaoFilter { Page = 2, PageSize = 10 };
            var serviceMock = new Mock<ISessaoService>();
            var expectedResult = new PaginatedResult<SessaoResponseDto>(
                new List<SessaoResponseDto> { new() { Id = 3, Tipo = "Avaliação" } },
                1, 2, 10
            );

            serviceMock.Setup(s => s.GetSessoesFiltradasAsync(30, filter))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await SessoesEndpoints.GetSessoesAsync(httpContext, filter, serviceMock.Object);

            // Assert
            var okResult = Assert.IsType<Ok<PaginatedResult<SessaoResponseDto>>>(result);
            Assert.Same(expectedResult, okResult.Value);
            serviceMock.Verify(s => s.GetSessoesFiltradasAsync(30, filter), Times.Once);
        }

        [Fact]
        public async Task SessoesEndpoints_GetSessoesAsync_ReturnsUnauthorized_WhenNoClaim()
        {
            // Arrange
            var httpContext = CreateHttpContextWithUser(null);
            var filter = new SessaoFilter();
            var serviceMock = new Mock<ISessaoService>();

            // Act
            var result = await SessoesEndpoints.GetSessoesAsync(httpContext, filter, serviceMock.Object);

            // Assert
            Assert.IsType<UnauthorizedHttpResult>(result);
            serviceMock.Verify(s => s.GetSessoesFiltradasAsync(It.IsAny<int>(), It.IsAny<SessaoFilter>()), Times.Never);
        }
    }
}
