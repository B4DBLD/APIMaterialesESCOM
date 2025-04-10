using APIMaterialesESCOM.Models;

namespace APIMaterialesESCOM.Repositorios
{
    public interface InterfazRepositorioCodigos
    {
        Task<CodigoVerificacion> CrearCodigoAsync(int userId, string codigo, DateTime expirationTime);
        Task<CodigoVerificacion> ObtenerCodigoAsync(string codigo);
        Task<bool> EliminarCodigoAsync(string codigo);
        Task<bool> EliminaCodigoUsuarioAsync(int usuarioId);
    }
}
