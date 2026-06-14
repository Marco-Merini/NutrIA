using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NutriFlow.Models;
using NutriFlow.Services;
using System.Threading.Tasks;

namespace NutriFlow.Endpoints
{
    public static class ProgressoEndpoints
    {
        public static void MapProgressoEndpoints(this IEndpointRouteBuilder routes)
        {
            routes.MapGet("/api/v1/progresso", async (
                HttpContext httpContext,
                [AsParameters] ProgressoFilter filter,
                IProgressoService service) =>
            {
                var userIdClaim = httpContext.User.FindFirst("UsuarioId")?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                    return Results.Unauthorized();

                var result = await service.GetProgressosFiltradosAsync(userId, filter);
                return Results.Ok(result);
            }).RequireAuthorization();
        }
    }
}
