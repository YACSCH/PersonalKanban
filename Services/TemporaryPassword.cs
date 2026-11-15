using System.Security.Cryptography;

namespace PersonalKanban.Services
{
    public static class TemporaryPassword
    {
        private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

        // Prefijo fijo para garantizar mayúscula, minúscula, dígito y símbolo.
        public static string Create() => "Aa1!" + RandomNumberGenerator.GetString(Chars, 12);
    }
}
