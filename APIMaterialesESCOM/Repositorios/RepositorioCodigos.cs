using APIMaterialesESCOM.Conexion;
using APIMaterialesESCOM.Models;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace APIMaterialesESCOM.Repositorios
{
    public class RepositorioCodigos : InterfazRepositorioCodigos
    {
        private readonly DBConfig _dbConfig;

        public RepositorioCodigos (DBConfig dbConfig)
        {
            _dbConfig = dbConfig;
        }

        public async Task<CodigoVerificacion> CrearCodigoAsync(int userId, string codigo, DateTime expirationTime)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO CodigoVerificacion (usuarioId, codigo, expires)
                VALUES (@userId, @codigo, @expires);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@codigo", codigo);
            command.Parameters.AddWithValue("@expires", expirationTime.ToString("o"));

            var id = Convert.ToInt32(await command.ExecuteScalarAsync());

            return new CodigoVerificacion
            {
                Id = id,
                UsuarioId = userId,
                Codigo = codigo,
                Expires = expirationTime
            };
        }

        public async Task<CodigoVerificacion> ObtenerCodigoAsync(string codigo)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, usuarioId, codigo, expires
                FROM CodigoVerificacion
                WHERE codigo = @codigo";

            command.Parameters.AddWithValue("@codigo", codigo);

            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                return new CodigoVerificacion
                {
                    Id = reader.GetInt32(0),
                    UsuarioId = reader.GetInt32(1),
                    Codigo = reader.GetString(2),
                    Expires = DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)
                };
            }

            return null;
        }

        public async Task<bool> EliminarCodigoAsync(string codigo)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM CodigoVerificacion WHERE codigo = @codigo";
            command.Parameters.AddWithValue("@codigo", codigo);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> EliminaCodigoUsuarioAsync(int usuarioId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM CodigoVerificacion WHERE usuarioId = @usuarioId";
            command.Parameters.AddWithValue("@usuarioId", usuarioId);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}
