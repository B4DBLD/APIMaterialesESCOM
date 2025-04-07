using APIMaterialesESCOM.Models;

namespace APIMaterialesESCOM.Repositorios
{
    public interface InterfazRepositorioLoginTokens
    {
        // Crea un nuevo token de inicio de sesión
        Task<LoginToken> CrearTokenAsync(int usuarioId, string token, DateTime fechaExpiracion);

        // Busca un token por su valor
        Task<LoginToken> ObtenerTokenAsync(string token);

        // Elimina un token específico
        Task<bool> EliminarTokenAsync(string token);

        // Elimina todos los tokens asociados a un usuario
        Task<bool> EliminarTokensUsuarioAsync(int usuarioId);

        // Verifica si el usuario tiene algún token válido
        Task<bool> TokenValidoAsync(int usuarioId);
    }
}
