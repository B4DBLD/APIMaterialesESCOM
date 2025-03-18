namespace APIMaterialesESCOM.Models
{
    // Clase que almacena la configuración para el servicio de correo electrónico
    // Estos valores se cargan desde el archivo appsettings.json
    public class EmailSettings
    {
        // Dirección de correo electrónico desde la cual se enviarán los mensajes
        // Debe ser una dirección verificada en el servicio de correo (Resend)
        public string Mail { get; set; } = string.Empty;

        // Nombre que aparecerá como remitente en los correos enviados
        // Ejemplo: "Repositorio Digital ESCOM"
        public string DisplayName { get; set; } = string.Empty;

        // Clave API del servicio de correo (Resend)
        // Se utiliza para autenticar las solicitudes de envío de correo
        public string ApiKey { get; set; } = string.Empty;
    }
}