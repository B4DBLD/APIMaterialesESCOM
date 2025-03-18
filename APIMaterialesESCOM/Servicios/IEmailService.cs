namespace APIMaterialesESCOM.Servicios
{
    // Interfaz que define el contrato para el servicio de envío de correos electrónicos
    // Permite desacoplar la implementación concreta del servicio de su uso en la aplicación
    public interface IEmailService
    {
        // Envía un correo electrónico de forma asíncrona
        // Parámetros:
        //   toEmail: Dirección de correo electrónico del destinatario
        //   subject: Asunto del correo electrónico
        //   message: Contenido HTML del correo electrónico
        // Retorna:
        //   true si el envío fue exitoso, false en caso contrario
        Task<bool> SendEmailAsync(string toEmail, string subject, string message);
    }
}