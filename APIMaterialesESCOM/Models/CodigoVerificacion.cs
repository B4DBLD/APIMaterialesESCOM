namespace APIMaterialesESCOM.Models
{
    public class CodigoVerificacion
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
    }
}
