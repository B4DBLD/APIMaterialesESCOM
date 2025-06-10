using APIMaterialesESCOM.Models;
using APIMaterialesESCOM.Repositorios;
using System.Text.Json;

namespace APIMaterialesESCOM.Servicios
{
    public class AutorBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutorBackgroundService> _logger;

        public AutorBackgroundService(IServiceProvider serviceProvider, ILogger<AutorBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcesarEventosPendientes();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando eventos en AutorBackgroundService");
                }

                await Task.Delay(5000, stoppingToken);
            }
        }

        private async Task ProcesarEventosPendientes()
        {
            using var scope = _serviceProvider.CreateScope();
            var outboxRepository = scope.ServiceProvider.GetRequiredService<InterfazRepositorioOutbox>();

            var eventos = await outboxRepository.GetPendingEventsAsync();

            foreach (var evento in eventos)
            {
                try
                {
                    await ProcesarEvento(evento);
                    await outboxRepository.MarkAsProcessedAsync(evento.Id);
                    _logger.LogInformation($"Evento {evento.EventType} procesado exitosamente para usuario {evento.UsuarioId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error procesando evento {evento.EventType} para usuario {evento.UsuarioId}");
                    await outboxRepository.IncrementRetryCountAsync(evento.Id);
                }
            }
        }

        private async Task ProcesarEvento(OutboxEvent evento)
        {
            var eventData = JsonSerializer.Deserialize<UsuarioEventData>(evento.EventData);
            if (eventData == null) return;

            switch (evento.EventType)
            {
                case "CREAR_AUTOR":
                    await ProcesarCrearAutor(eventData);
                    break;
                case "ELIMINAR_RELACION":
                    await ProcesarEliminarRelacion(eventData);
                    break;
                default:
                    _logger.LogWarning($"Tipo de evento desconocido: {evento.EventType}");
                    break;
            }
        }

        private async Task ProcesarCrearAutor(UsuarioEventData eventData)
        {
            try
            {
                var apiCliente = new ApiRequest();
                int autorID = 0;

                // Obtener autor existente
                var response = await apiCliente.ObtenerAutor(eventData.Email);
                if (response?.Ok == true)
                {
                    autorID = response.Data.Id;
                }
                else
                {
                    // Crear nuevo autor
                    var autor = new Autor
                    {
                        Nombre = eventData.Nombre,
                        ApellidoP = eventData.ApellidoP,
                        ApellidoM = eventData.ApellidoM,
                        Email = eventData.Email
                    };

                    var createResponse = await apiCliente.CrearAutor(autor);
                    if (createResponse.Ok)
                    {
                        autorID = createResponse.Data.Id;
                    }
                    else
                    {
                        throw new Exception("Error al crear el autor en el sistema externo");
                    }
                }

                // Crear relación
                var relacionResponse = await apiCliente.CrearRelacion(eventData.UsuarioId, autorID);
                if (!relacionResponse.Ok)
                {
                    throw new Exception("Error al crear la relación entre el usuario y el autor");
                }

                _logger.LogInformation($"Autor creado/vinculado exitosamente para usuario {eventData.UsuarioId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en ProcesarCrearAutor para usuario {eventData.UsuarioId}");
                throw;
            }
        }

        private async Task ProcesarEliminarRelacion(UsuarioEventData eventData)
        {
            try
            {
                var apiCliente = new ApiRequest();

                // Obtener relación existente
                var getRelacionResponse = await apiCliente.GetRelacion(eventData.UsuarioId);
                if (getRelacionResponse.Ok)
                {
                    int autorID = getRelacionResponse.Data.Id;
                    var relacionResponse = await apiCliente.EliminarRelacion(eventData.UsuarioId, autorID);
                    
                    if (!relacionResponse.Ok)
                    {
                        throw new Exception("Error al eliminar la relación entre el usuario y el autor");
                    }

                    _logger.LogInformation($"Relación eliminada exitosamente para usuario {eventData.UsuarioId}");
                }
                else
                {
                    _logger.LogWarning($"No se encontró relación para eliminar del usuario {eventData.UsuarioId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en ProcesarEliminarRelacion para usuario {eventData.UsuarioId}");
                throw;
            }
        }
    }
}