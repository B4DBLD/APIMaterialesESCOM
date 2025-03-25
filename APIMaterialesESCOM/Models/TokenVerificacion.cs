namespace APIMaterialesESCOM.Models
{
    public class TokenVerificacion
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
    }
}
