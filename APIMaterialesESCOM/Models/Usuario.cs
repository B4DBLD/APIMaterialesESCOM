using APIMaterialesESCOM.Servicios;
using APIMaterialesESCOM.Validacion;
using System.ComponentModel.DataAnnotations;

namespace APIMaterialesESCOM.Models
{
    // Representa un usuario del sistema con todos sus datos
    public class Usuario
    {
        // Identificador único del usuario
        public int Id { get; set; }

        // Nombre del usuario
        public string Nombre { get; set; } = string.Empty;

        // Apellido paterno
        public string ApellidoP { get; set; } = string.Empty;

        // Apellido materno
        public string ApellidoM { get; set; } = string.Empty;

        // Correo electrónico (debe ser único en el sistema)
        public string Email { get; set; } = string.Empty;

        // Número de boleta o identificación escolar (puede ser nulo)
        public string? Boleta { get; set; }

        // Rol del usuario en el sistema (estudiante, profesor, administrador)
        public string Rol { get; set; } = string.Empty;

        // Fecha y hora de creación del registro
        public string FechaCreacion { get; set; } = string.Empty;

        // Fecha y hora de la última actualización del registro
        public string FechaActualizacion { get; set; } = string.Empty;
        public bool VerificacionEmail { get; set; }
    }

    // DTO para operaciones de inicio de sesión
    public class UsuarioSignIn
    {
        // Correo electrónico para autenticación
        public string Email { get; set; } = string.Empty;
    }

    // DTO para operaciones de registro de usuarios
    public class UsuarioSignUp
    {
        // Nombre del usuario (campo obligatorio)
        [Required(ErrorMessage = "El nombre es requerido")]
        public string Nombre { get; set; } = string.Empty;

        // Apellido paterno (campo obligatorio)
        [Required(ErrorMessage = "El apellido paterno es requerido")]
        public string ApellidoP { get; set; } = string.Empty;

        // Apellido materno
        public string ApellidoM { get; set; } = string.Empty;

        // Correo electrónico con validaciones de formato y dominio
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [Validacion] // Validación personalizada para restricción de dominios
        public string Email { get; set; } = string.Empty;

        // Boleta o identificación (campo obligatorio)
        [Required(ErrorMessage = "La boleta es requerida")]
        public string? Boleta { get; set; }
    }

    // DTO para operaciones de actualización de usuarios
    public class UsuarioUpdate
    {
        // Campos opcionales que pueden ser actualizados
        public string? Nombre { get; set; }
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