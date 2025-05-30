namespace MicroserviciosRepoEscom.Models
{
    public class Respuesta<T>
    {
        public bool Ok { get; set; }
        public T? Data { get; set; } // Nullable if T is a reference type or Nullable<value type>
        public string? Message { get; set; } // General message, can be used for errors or success messages
        public List<string>? Errors { get; set; } // For detailed error messages or validation errors

        // Static factory methods for convenience

        public static Respuesta<T> Success(T data, string? message = null)
        {
            return new Respuesta<T> { Ok = true, Data = data, Message = message };
        }

        public static Respuesta<object> Failure(string message, List<string>? errors = null) // Non-generic for failures where T might not be relevant
        {
            return new Respuesta<object> { Ok = false, Message = message, Errors = errors, Data = null };
        }

        // If you want a failure method that still conforms to Respuesta<T>
        public static Respuesta<T> Failure(string message, List<string>? errors = null, T? data = default)
        {
            return new Respuesta<T> { Ok = false, Message = message, Errors = errors, Data = data };
        }
    }

    public class Respuesta
    {
        public bool Ok { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }

        public static Respuesta Success(string? message = null)
        {
            return new Respuesta { Ok = true, Message = message };
        }

        public static Respuesta Failure(string message, List<string>? errors = null)
        {
            return new Respuesta { Ok = false, Message = message, Errors = errors };
        }
    }
}
