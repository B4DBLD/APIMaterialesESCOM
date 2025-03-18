//Clase de configuración para la conexión a la base de datos.
//Contiene la cadena de conexión utilizada para conectarse a SQLite.

namespace APIMaterialesESCOM.Conexion
{
    public class DBConfig
    {
        // Obtiene o establece la cadena de conexión a la base de datos SQLite.
        // Esta propiedad es configurada en Program.cs a partir de appsettings.json.
        // Ejemplo: "Data Source=./DBRepositorioESCOM.db"
        public string ConnectionString { get; set; } = string.Empty;
    }
}
