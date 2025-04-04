namespace APIMaterialesESCOM.Servicios
{
    public interface ITokenService
    {
        string GenerateToken();
        bool ValidateToken(string token);
        DateTime GetExpirationTime();
        bool IsTokenExpired(DateTime expirationTime);
    }
}
