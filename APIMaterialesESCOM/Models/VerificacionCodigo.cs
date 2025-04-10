using System.ComponentModel.DataAnnotations;

namespace APIMaterialesESCOM.Models
{
    public class VerificacionCodigo
    {
        [Required]
        public string Codigo { get; set; }

        [Required]
        public int UsuarioId { get; set; }
    }
}
