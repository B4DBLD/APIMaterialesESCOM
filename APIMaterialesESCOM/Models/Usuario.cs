using APIMaterialesESCOM.Servicios;
using APIMaterialesESCOM.Validacion;
using System.ComponentModel.DataAnnotations;

namespace APIMaterialesESCOM.Models
{
    public class Usuario // Representa un usuario del sistema con todos sus datos
    {
        public int Id { get; set; } // Identificador único del usuario
        public string Nombre { get; set; } = string.Empty; // Nombre del usuario
        public string ApellidoP { get; set; } = string.Empty; // Apellido paterno
        public string ApellidoM { get; set; } = string.Empty; // Apellido materno
        public string Email { get; set; } = string.Empty; // Correo electrónico (debe ser único en el sistema)
        public string? Boleta { get; set; } = string.Empty; // Número de boleta
        public string Rol { get; set; } = string.Empty; // Rol del usuario en el sistema (estudiante, profesor, administrador)
        public string FechaCreacion { get; set; } = string.Empty; // Fecha y hora de creación del registro
        public string FechaActualizacion { get; set; } = string.Empty; // Fecha y hora de la última actualización del registro
        public bool VerificacionEmail { get; set; } // Validación de email
    }

    public class UsuarioSignIn // DTO para operaciones de inicio de sesión
    {
        public string Email { get; set; } = string.Empty; // Correo electrónico para autenticación
    }

    public class UsuarioSignUp // DTO para operaciones de registro de usuarios
    {
        [Required(ErrorMessage = "El nombre es requerido")] // Nombre del usuario (campo obligatorio)
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido paterno es requerido")] // Apellido paterno (campo obligatorio)
        public string ApellidoP { get; set; } = string.Empty;
        public string ApellidoM { get; set; } = string.Empty; // Apellido materno

        // Correo electrónico con validaciones de formato y dominio
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [Validacion] // Validación personalizada para restricción de dominios
        public string Email { get; set; } = string.Empty;

        // Boleta o identificación (campo obligatorio)
        [Required(ErrorMessage = "La boleta es requerida")]
        public string? Boleta { get; set; }
    }

    public class UsuarioUpdate // DTO para operaciones de actualización de usuarios
    {
        public string? Nombre { get; set; } // Campos opcionales que pueden ser actualizados
        public string? ApellidoP { get; set; }
        public string? ApellidoM { get; set; }

        // El email debe cumplir con el formato estándar y la validación de dominio
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [Validacion] // Validación personalizada para restricción de dominios
        public string? Email { get; set; }

        public string? Boleta { get; set; }
        public string? Rol { get; set; }
    }
}