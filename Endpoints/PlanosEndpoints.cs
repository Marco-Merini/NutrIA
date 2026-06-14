using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NutriFlow.Models;
using NutriFlow.Services;
using System.Threading.Tasks;

namespace NutriFlow.Endpoints
{
    public static class PlanosEndpoints
    {
        public static void MapPlanosEndpoints(this IEndpointRouteBuilder routes)
        {
            routes.MapGet("/api/v1/planos", GetPlanosAsync).RequireAuthorization();
        }

        public static async Task<IResult> GetPlanosAsync(
            HttpContext httpContext,
            [AsParameters] PlanoDietaFilter filter,
            IPlanoDietaService service)
        {
            var userIdClaim = httpContext.User.FindFirst("UsuarioId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Results.Unauthorized();

            var result = await service.GetPlanosFiltradosAsync(userId, filter);
            return Results.Ok(result);
        }
    }
}
