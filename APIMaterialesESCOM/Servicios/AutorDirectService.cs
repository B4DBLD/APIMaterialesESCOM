using APIMaterialesESCOM.Models;

namespace APIMaterialesESCOM.Servicios
{
    public class AutorDirectService : IAutorDirectService
    {
        private readonly ILogger<AutorDirectService> _logger;

        public AutorDirectService(ILogger<AutorDirectService> logger)
        {
            _logger = logger;
        }

        public async Task<int> ObtenerAutorIdAsync(int usuarioId)
        {
            try
            {
                var apiCliente = new ApiRequest();
                var relacionResponse = await apiCliente.GetRelacion(usuarioId);
                
                if (relacionResponse?.Ok == true)
                {
                    return relacionResponse.Data.Id;
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo autorId para usuario {usuarioId}");
                return 0;
            }
        }

        public string DeterminarTipoEventoActualizacion(string rolAnterior, string rolNuevo)
        {
            bool teniaRelacion = rolAnterior == "2" || rolAnterior == "3";
            bool necesitaRelacion = rolNuevo == "2" || rolNuevo == "3";

            return (teniaRelacion, necesitaRelacion) switch
            {
                (false, true) => "CREAR_AUTOR",
                (true, false) => "ELIMINAR_RELACION",
                (true, true) => "SIN_CAMBIOS",
                _ => "SIN_CAMBIOS"
            };
        }
    }
}