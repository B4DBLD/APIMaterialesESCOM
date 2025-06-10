using APIMaterialesESCOM.Conexion;
using APIMaterialesESCOM.Models;
using Microsoft.Data.Sqlite;
using System.Data;

namespace APIMaterialesESCOM.Repositorios
{
    public class RepositorioOutbox : InterfazRepositorioOutbox
    {
        private readonly DBConfig _conexion;

        public RepositorioOutbox(DBConfig conexion)
        {
            _conexion = conexion;
        }

        public async Task AddEventAsync(string eventType, string eventData, int usuarioId)
        {
            using var connection = new SqliteConnection(_conexion.ConnectionString);
            var query = @"
                INSERT INTO OutboxEvents (EventType, EventData, UsuarioId, CreatedAt, Processed, RetryCount)
                VALUES (@EventType, @EventData, @UsuarioId, datetime('now'), 0, 0)";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@EventType", eventType);
            command.Parameters.AddWithValue("@EventData", eventData);
            command.Parameters.AddWithValue("@UsuarioId", usuarioId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<OutboxEvent>> GetPendingEventsAsync()
        {
            using var connection = new SqliteConnection(_conexion.ConnectionString);
            var query = @"
                SELECT Id, EventType, EventData, UsuarioId, CreatedAt, Processed, RetryCount
                FROM OutboxEvents 
                WHERE Processed = 0 AND RetryCount < 5
                ORDER BY CreatedAt
                LIMIT 10";

            using var command = new SqliteCommand(query, connection);
            await connection.OpenAsync();

            var events = new List<OutboxEvent>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                events.Add(new OutboxEvent
                {
                    Id = reader.GetInt32("Id"),
                    EventType = reader.GetString("EventType"),
                    EventData = reader.GetString("EventData"),
                    UsuarioId = reader.GetInt32("UsuarioId"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    Processed = reader.GetBoolean("Processed"),
                    RetryCount = reader.GetInt32("RetryCount")
                });
            }

            return events;
        }

        public async Task MarkAsProcessedAsync(int eventId)
        {
            using var connection = new SqliteConnection(_conexion.ConnectionString);
            var query = "UPDATE OutboxEvents SET Processed = 1 WHERE Id = @Id";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", eventId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task IncrementRetryCountAsync(int eventId)
        {
            using var connection = new SqliteConnection(_conexion.ConnectionString);
            var query = "UPDATE OutboxEvents SET RetryCount = RetryCount + 1 WHERE Id = @Id";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", eventId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
    }
}