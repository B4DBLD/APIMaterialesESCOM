using APIMaterialesESCOM.Conexion;
using APIMaterialesESCOM.Models;
using Microsoft.Data.Sqlite;

namespace APIMaterialesESCOM.Repositorios
{
    public class RepositorioLoginTokens : InterfazRepositorioLoginTokens
    {
        private readonly DBConfig _dbConfig;

        public RepositorioLoginTokens(DBConfig dbConfig)
        {
            _dbConfig = dbConfig;
        }

        public async Task<LoginToken> CrearTokenAsync(int usuarioId, string token, DateTime fechaExp)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO LoginToken (userId, token, expires)
                VALUES (@userId, @token, @expires);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@userId", usuarioId);
            command.Parameters.AddWithValue("@token", token);
            command.Parameters.AddWithValue("@expires", fechaExp.ToString("o"));

            var id = Convert.ToInt32(await command.ExecuteScalarAsync());

            return new LoginToken
            {
                Id = id,
                UsuarioId = usuarioId,
                Token = token,
                Expires = fechaExp,
                Creado = DateTime.Now
            };
        }

        public async Task<LoginToken> ObtenerTokenAsync(string token)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, userId, token, expires, createdAt
                FROM LoginToken
                WHERE token = @token";

            command.Parameters.AddWithValue("@token", token);

            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                return new LoginToken
                {
                    Id = reader.GetInt32(0),
                    UsuarioId = reader.GetInt32(1),
                    Token = reader.GetString(2),
                    Expires = DateTime.Parse(reader.GetString(3)),
                    Creado = DateTime.Parse(reader.GetString(4))
                };
            }

            return null;
        }

        public async Task<bool> EliminarTokenAsync(string token)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM LoginToken WHERE token = @token";
            command.Parameters.AddWithValue("@token", token);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> EliminarTokensUsuarioAsync(int usuarioId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM LoginToken WHERE userId = @userId";
            command.Parameters.AddWithValue("@userId", usuarioId);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> TokenValidoAsync(int usuarioId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*)
                FROM LoginToken
                WHERE userId = @userId AND expires > @now";

            command.Parameters.AddWithValue("@userId", usuarioId);
            command.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));

            int count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }
    }
}
