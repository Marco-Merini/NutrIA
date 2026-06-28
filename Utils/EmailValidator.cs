using System.Text.RegularExpressions;

namespace NutriFlow.Utils
{
    public static class EmailValidator
    {
        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.None,
            TimeSpan.FromMilliseconds(500));

        public static string? Validate(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "O email é obrigatório";

            return EmailRegex.IsMatch(email)
                ? null
                : "Informe um email válido (ex: usuario@gmail.com)";
        }
    }
}
