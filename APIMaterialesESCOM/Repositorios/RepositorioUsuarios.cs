using APIMaterialesESCOM.Conexion;
using APIMaterialesESCOM.Models;
using Microsoft.Data.Sqlite;
using System.Text;

namespace APIMaterialesESCOM.Repositorios
{
    // Implementación concreta del repositorio de usuarios que accede a una base de datos SQLite
    public class RepositorioUsuarios : InterfazRepositorioUsuarios
    {
        private readonly DBConfig _dbConfig;

        // Constructor que recibe la configuración de conexión a la base de datos
        public RepositorioUsuarios(DBConfig dbConfig)
        {
            _dbConfig = dbConfig;
        }

        // Obtiene todos los usuarios registrados en el sistema
        public async Task<IEnumerable<Usuario>> GetAllUsuarios()
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
               SELECT id, nombre, apellidoP, apellidoM, email, boleta, rol, fechaCreacion, fechaActualizacion, emailVerified 
               FROM Usuario";

            using var reader = await command.ExecuteReaderAsync();
            var usuarios = new List<Usuario>();

            while(await reader.ReadAsync())
            {
                usuarios.Add(new Usuario
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    ApellidoP = reader.GetString(2),
                    ApellidoM = reader.GetString(3),
                    Email = reader.GetString(4),
                    Boleta = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Rol = reader.GetString(6),
                    FechaCreacion = reader.GetString(7),
                    FechaActualizacion = reader.GetString(8),
                    VerificacionEmail = reader.GetInt32(9) == 1
                });
            }

            return usuarios;
        }

        // Busca un usuario específico por su ID
        public async Task<Usuario?> GetUsuarioById(int id)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
               SELECT id, nombre, apellidoP, apellidoM, email, boleta, rol, fechaCreacion, fechaActualizacion, emailVerified
               FROM Usuario 
               WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                return new Usuario
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    ApellidoP = reader.GetString(2),
                    ApellidoM = reader.GetString(3),
                    Email = reader.GetString(4),
                    Boleta = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Rol = reader.GetString(6),
                    FechaCreacion = reader.GetString(7),
                    FechaActualizacion = reader.GetString(8),
                    VerificacionEmail = reader.GetInt32(9) == 1
                };
            }

            return null;
        }

        // Busca un usuario por su dirección de correo electrónico
        public async Task<Usuario?> GetUsuarioByEmail(string email)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
               SELECT id, nombre, apellidoP, apellidoM, email, boleta, rol, fechaCreacion, fechaActualizacion, emailVerified 
               FROM Usuario 
               WHERE email = @email";
            command.Parameters.AddWithValue("@email", email);

            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                return new Usuario
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    ApellidoP = reader.GetString(2),
                    ApellidoM = reader.GetString(3),
                    Email = reader.GetString(4),
                    Boleta = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Rol = reader.GetString(6),
                    FechaCreacion = reader.GetString(7),
                    FechaActualizacion = reader.GetString(8),
                    VerificacionEmail = reader.GetInt32(9) == 1
                };
            }

            return null;
        }

        // Crea un nuevo usuario en el sistema
        public async Task<int> CreateUsuario(UsuarioSignUp usuario)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
               INSERT INTO Usuario (nombre, apellidoP, apellidoM, email, boleta, rol, fechaCreacion, fechaActualizacion)
               VALUES (@nombre, @apellidoP, @apellidoM, @email, @boleta, 'estudiante', datetime('now', 'utc'), datetime('now', 'utc'));
               SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@nombre", usuario.Nombre);
            command.Parameters.AddWithValue("@apellidoP", usuario.ApellidoP);
            command.Parameters.AddWithValue("@apellidoM", usuario.ApellidoM);
            command.Parameters.AddWithValue("@email", usuario.Email);
            command.Parameters.AddWithValue("@boleta", usuario.Boleta as object ?? DBNull.Value);

            long newId = (long)await command.ExecuteScalarAsync();
            return (int)newId;
        }

        // Actualiza la información de un usuario existente
        public async Task<bool> UpdateUsuario(int id, UsuarioUpdate usuario)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            // Construir la consulta SQL dinámicamente para actualizar solo los campos proporcionados
            var sqlBuilder = new StringBuilder("UPDATE Usuario SET fechaActualizacion = datetime('now', 'utc')");

            if(!string.IsNullOrEmpty(usuario.Nombre))
                sqlBuilder.Append(", nombre = @nombre");

            if(!string.IsNullOrEmpty(usuario.ApellidoP))
                sqlBuilder.Append(", apellidoP = @apellidoP");

            if(!string.IsNullOrEmpty(usuario.ApellidoM))
                sqlBuilder.Append(", apellidoM = @apellidoM");

            if(!string.IsNullOrEmpty(usuario.Email))
                sqlBuilder.Append(", email = @email");

            if(usuario.Boleta != null)
                sqlBuilder.Append(", boleta = @boleta");

            if(!string.IsNullOrEmpty(usuario.Rol))
                sqlBuilder.Append(", rol = @rol");

            sqlBuilder.Append(" WHERE id = @id");

            using var command = connection.CreateCommand();
            command.CommandText = sqlBuilder.ToString();
            command.Parameters.AddWithValue("@id", id);

            // Añadir solo los parámetros de los campos que se van a actualizar
            if(!string.IsNullOrEmpty(usuario.Nombre))
                command.Parameters.AddWithValue("@nombre", usuario.Nombre);

            if(!string.IsNullOrEmpty(usuario.ApellidoP))
                command.Parameters.AddWithValue("@apellidoP", usuario.ApellidoP);

            if(!string.IsNullOrEmpty(usuario.ApellidoM))
                command.Parameters.AddWithValue("@apellidoM", usuario.ApellidoM);

            if(!string.IsNullOrEmpty(usuario.Email))
                command.Parameters.AddWithValue("@email", usuario.Email);

            if(usuario.Boleta != null)
                command.Parameters.AddWithValue("@boleta", usuario.Boleta as object ?? DBNull.Value);

            if(!string.IsNullOrEmpty(usuario.Rol))
                command.Parameters.AddWithValue("@rol", usuario.Rol);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        // Elimina un usuario del sistema
        public async Task<bool> DeleteUsuario(int id)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Usuario WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        // Autentica a un usuario utilizando email y boleta
        public async Task<Usuario?> Authenticate(string email)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
               SELECT id, nombre, apellidoP, apellidoM, email, boleta, rol, emailVerified 
               FROM Usuario 
               WHERE email = @email";
            command.Parameters.AddWithValue("@email", email);

            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                return new Usuario
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    ApellidoP = reader.GetString(2),
                    ApellidoM = reader.GetString(3),
                    Email = reader.GetString(4),
                    Boleta = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Rol = reader.GetString(6),
                    VerificacionEmail = reader.GetInt32(7) == 1
                };
            }

            return null;
        }

        public async Task<bool> VerificacionEmailAsync(int userId, bool verified)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Usuario SET emailVerified = @verified, fechaActualizacion = datetime('now', 'utc') WHERE id = @id";
            command.Parameters.AddWithValue("@verified", verified ? 1 : 0);
            command.Parameters.AddWithValue("@id", userId);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> EmailVerificadoAsync(int userId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT emailVerified FROM Usuario WHERE id = @id";
            command.Parameters.AddWithValue("@id", userId);

            var result = await command.ExecuteScalarAsync();

            return result != null && Convert.ToInt32(result) == 1;
        }
    }
}