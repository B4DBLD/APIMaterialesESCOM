namespace APIMaterialesESCOM.Models
{
    public class OutboxEvent
    {
        public int Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EventData { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Processed { get; set; }
        public int RetryCount { get; set; }
    }

    public class UsuarioEventData
    {
        public int UsuarioId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string ApellidoP { get; set; } = string.Empty;
        public string ApellidoM { get; set; } = string.Empty;
        public string RolAnterior { get; set; } = string.Empty;
        public string RolNuevo { get; set; } = string.Empty;
    }
}