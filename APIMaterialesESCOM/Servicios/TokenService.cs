using System.Security.Cryptography;

namespace APIMaterialesESCOM.Servicios
{
    public class TokenService : ITokenService
    {
        // Genera un token aleatorio único
        public string GenerateToken()
        {
            // Crear un array de bytes aleatorios
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            // Convertir a string hexadecimal para uso como token
            return Convert.ToHexString(randomBytes);
        }

        public bool ValidateToken(string token)
        {
            // Implementación básica para validar un token
            return !string.IsNullOrEmpty(token);
        }

        // Calcula la fecha de expiración (ejemplo: 24 horas desde la creación)
        public DateTime GetExpirationTime()
        {
            return DateTime.UtcNow.AddHours(24);
        }

        public DateTime GetExpirationTimeLogin()
        {
            return DateTime.UtcNow.AddMinutes(15);
        }

        // Valida si un token ha expirado
        public bool IsTokenExpired(DateTime expirationTime)
        {
            return DateTime.UtcNow > expirationTime;
        }
    }
}
