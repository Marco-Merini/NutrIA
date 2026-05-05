using System.Text;

namespace NutriFlow.Services
{
    /// <summary>
    /// Provides the JWT signing key from secure sources (environment variable or user-secrets).
    /// This class isolates secret retrieval from cryptographic usage to satisfy
    /// SonarCloud rule csharpsquid:S6781.
    /// </summary>
    public static class JwtKeyProvider
    {
        public static byte[] GetKey(IConfiguration configuration)
        {
            var secret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? configuration["Jwt:Key"];

            if (string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException(
                    "JWT secret key not configured. Set the 'JWT_SECRET_KEY' environment variable or use 'dotnet user-secrets set \"Jwt:Key\" \"<your-key>\"'.");
            }

            return Encoding.UTF8.GetBytes(secret);
        }
    }
}
