namespace APIMaterialesESCOM.Servicios
{
    public interface IAutorDirectService
    {
        Task<int> ObtenerAutorIdAsync(int usuarioId);
        string DeterminarTipoEventoActualizacion(string rolAnterior, string rolNuevo);
    }
}