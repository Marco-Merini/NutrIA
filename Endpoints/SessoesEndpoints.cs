using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NutriFlow.Models;
using NutriFlow.Services;
using System.Threading.Tasks;

namespace NutriFlow.Endpoints
{
    public static class SessoesEndpoints
    {
        public static void MapSessoesEndpoints(this IEndpointRouteBuilder routes)
        {
            routes.MapGet("/api/v1/sessoes", async (
                HttpContext httpContext,
                [AsParameters] SessaoFilter filter,
                ISessaoService service) =>
            {
                var userIdClaim = httpContext.User.FindFirst("UsuarioId")?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                    return Results.Unauthorized();

                var result = await service.GetSessoesFiltradasAsync(userId, filter);
                return Results.Ok(result);
            }).RequireAuthorization();
        }
    }
}
