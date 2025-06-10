using System.ComponentModel.DataAnnotations;

namespace APIMaterialesESCOM.Models
{
    public class Autor
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido paterno es requerido")]
        public string ApellidoP { get; set; } = string.Empty;

        public string? ApellidoM { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string Email { get; set; } = string.Empty;
    }

}
