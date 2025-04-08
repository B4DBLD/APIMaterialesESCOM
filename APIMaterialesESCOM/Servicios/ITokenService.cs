namespace APIMaterialesESCOM.Servicios
{
    public interface ITokenService
    {
        string GenerateToken();
        bool ValidateToken(string token);
        DateTime GetExpirationTime();
        DateTime GetExpirationJWT();
        bool IsTokenExpired(DateTime expirationTime);
    }
}
