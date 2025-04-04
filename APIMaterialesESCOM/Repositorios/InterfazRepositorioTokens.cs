using APIMaterialesESCOM.Models;

namespace APIMaterialesESCOM.Repositorios
{
    public interface InterfazRepositorioTokens
    {
        Task<TokenVerificacion> CreateTokenAsync(int userId, string token, DateTime expirationTime);
        Task<TokenVerificacion> GetTokenAsync(string token);
        Task<bool> DeleteTokenAsync(string token);
    }
}
