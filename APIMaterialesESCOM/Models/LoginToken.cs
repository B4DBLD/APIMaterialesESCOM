namespace APIMaterialesESCOM.Models
{
    // Modelo que representa un token de autenticación para inicio de sesión
    public class LoginToken
    {

        // Identificador único del token
        public int Id { get; set; }

        // ID del usuario al que pertenece este token
        public int UsuarioId { get; set; }

        // Valor único del token
        public string Token { get; set; } = string.Empty;

        // Fecha y hora de expiración del token
        public DateTime Expires { get; set; }

        // Fecha y hora de creación del token
        public DateTime Creado { get; set; }

    }
}
