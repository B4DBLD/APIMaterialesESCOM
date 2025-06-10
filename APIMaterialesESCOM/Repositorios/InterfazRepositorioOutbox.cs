using APIMaterialesESCOM.Models;

namespace APIMaterialesESCOM.Repositorios
{
    public interface InterfazRepositorioOutbox
    {
        Task AddEventAsync(string eventType, string eventData, int usuarioId);
        Task<IEnumerable<OutboxEvent>> GetPendingEventsAsync();
        Task MarkAsProcessedAsync(int eventId);
        Task IncrementRetryCountAsync(int eventId);
    }
}