using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NutriFlow.Services
{
    public class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public JwtAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return Task.FromResult(new AuthenticationState(_anonymous));
            }

            var token = httpContext.Request.Cookies["NutriAI.AuthToken"];

            if (string.IsNullOrWhiteSpace(token))
            {
                return Task.FromResult(new AuthenticationState(_anonymous));
            }

            var claims = ParseClaimsFromJwt(token);
            if (claims == null)
            {
                return Task.FromResult(new AuthenticationState(_anonymous));
            }

            // Check if token is expired
            var expClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp 
                || c.Type == "exp");
            if (expClaim != null && long.TryParse(expClaim.Value, out long expUnix))
            {
                var expDate = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                if (expDate < DateTime.UtcNow)
                {
                    return Task.FromResult(new AuthenticationState(_anonymous));
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            return Task.FromResult(new AuthenticationState(user));
        }

        public void NotifyAuthStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private static IEnumerable<Claim>? ParseClaimsFromJwt(string jwt)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwt);
                return token.Claims;
            }
            catch
            {
                return null;
            }
        }
    }
}
