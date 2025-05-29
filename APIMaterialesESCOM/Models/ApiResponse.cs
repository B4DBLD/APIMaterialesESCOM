using System.Text.Json.Serialization;

namespace APIMaterialesESCOM.Models
{
    public class ApiResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("data")]
        public AutorData Data { get; set; } // El objeto anidado que contiene el Id

        [JsonPropertyName("message")]
        public string Message { get; set; } // Aunque sea null, es bueno tenerlo por si cambia

        [JsonPropertyName("errors")]
        public object Errors { get; set; } // object es flexible para null o estructuras de error
    }
    public class AutorData // Podrías llamarla AutorDetails o similar
    {
        [JsonPropertyName("id")] // Asegura el mapeo correcto si usas casing diferente en C#
        public int Id { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; }

        [JsonPropertyName("apellido")]
        public string Apellido { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("fechaCreacion")]
        public string FechaCreacion { get; set; } // O DateTime si quieres parsearlo

        [JsonPropertyName("fechaActualizacion")]
        public string FechaActualizacion { get; set; } // O DateTime
    }
}
