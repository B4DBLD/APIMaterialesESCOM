namespace APIMaterialesESCOM.Servicios
{
    public interface ICodeService
    {
        string GenerarCodigo();
        DateTime TiempoExpiracion();
        DateTime TiempoExpiracionJWT();
        bool ExpiracionCodigo(DateTime expirationTime);
    }
}
