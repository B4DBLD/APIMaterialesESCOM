using APIMaterialesESCOM.Validacion;
using System.ComponentModel.DataAnnotations;

namespace APIMaterialesESCOM.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string ApellidoP { get; set; } = string.Empty;
        public string ApellidoM { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Boleta { get; set; }
        public string Rol { get; set; } = string.Empty;
        public string FechaCreacion { get; set; } = string.Empty;
        public string FechaActualizacion { get; set; } = string.Empty;
    }

    // DTOs para operaciones específicas
    public class UsuarioSignIn
    {
        public string Email { get; set; } = string.Empty;
        public string Boleta { get; set; } = string.Empty;
    }

    public class UsuarioSignUp
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido paterno es requerido")]
        public string ApellidoP { get; set; } = string.Empty;
        public string ApellidoM { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [Validacion]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La boleta es requerida")]
        public string? Boleta { get; set; }
    }

    public class UsuarioUpdate
    {
        public string? Nombre { get; set; }
        public string? ApellidoP { get; set; }
        public string? ApellidoM { get; set; }

        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [Validacion]
        public string? Email { get; set; }
        public string? Boleta { get; set; }
        public string? Rol { get; set; }
    }
}
