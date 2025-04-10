using System.Security.Cryptography;

namespace APIMaterialesESCOM.Servicios
{
    public class CodeService : ICodeService
    {
        private readonly Random _random = new Random();

        public string GenerarCodigo() // Método para generar códigos numéricos de 6 dígitos
        {
            return _random.Next(100000, 999999).ToString();
        }

        
        public DateTime TiempoExpiracion() // Calcula la fecha de expiración (ejemplo: 24 horas desde la creación)
        {
            return DateTime.UtcNow.AddHours(1);
        }

        public DateTime TiempoExpiracionJWT()
        {
            return DateTime.UtcNow.AddDays(30);
        }

        public bool ExpiracionCodigo(DateTime expirationTime)  // Valida si un código ha expirado
        {
            return DateTime.UtcNow > expirationTime;
        }
    }
}
