namespace APIMaterialesESCOM.Servicios
{
    public interface ITokenService
    {
        string GenerateToken();
        bool ValidateToken(string token);
        DateTime GetExpirationTime();
        DateTime GetExpirationTimeLogin();
        bool IsTokenExpired(DateTime expirationTime);
    }
}
