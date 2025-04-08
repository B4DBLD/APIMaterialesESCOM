using APIMaterialesESCOM.Conexion;
using APIMaterialesESCOM.Models;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace APIMaterialesESCOM.Repositorios
{
    public class RepositorioTokens : InterfazRepositorioTokens
    {
        private readonly DBConfig _dbConfig;

        public RepositorioTokens (DBConfig dbConfig)
        {
            _dbConfig = dbConfig;
        }

        public async Task<TokenVerificacion> CreateTokenAsync(int userId, string token, DateTime expirationTime)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO TokenVerificacion (usuarioId, token, expires)
                VALUES (@userId, @token, @expires);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@token", token);
            command.Parameters.AddWithValue("@expires", expirationTime.ToString("o"));

            var id = Convert.ToInt32(await command.ExecuteScalarAsync());

            return new TokenVerificacion
            {
                Id = id,
                UsuarioId = userId,
                Token = token,
                Expires = expirationTime
            };
        }

        public async Task<TokenVerificacion> GetTokenAsync(string token)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, usuarioId, token, expires
                FROM TokenVerificacion
                WHERE token = @token";

            command.Parameters.AddWithValue("@token", token);

            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                return new TokenVerificacion
                {
                    Id = reader.GetInt32(0),
                    UsuarioId = reader.GetInt32(1),
                    Token = reader.GetString(2),
                    Expires = DateTime.Parse(reader.GetString(3))
                };
            }

            return null;
        }

        public async Task<bool> DeleteTokenAsync(string token)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM TokenVerificacion WHERE token = @token";
            command.Parameters.AddWithValue("@token", token);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}
