namespace APIMaterialesESCOM.Servicios
{
    public class ITokenService
    {
        string GenerateToken();
        bool ValidateToken(string token);
    }
}
