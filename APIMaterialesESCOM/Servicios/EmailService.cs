// Services/EmailService.cs
using APIMaterialesESCOM.Models;
using APIMaterialesESCOM.Servicios;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace APIMaterialesESCOM.Services
{
    // Implementación del servicio de correo electrónico utilizando la API REST de Resend
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly HttpClient _httpClient;

        // Constructor que configura el cliente HTTP para comunicarse con la API de Resend
        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("ResendApi");
            _httpClient.BaseAddress = new Uri("https://api.resend.com");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _emailSettings.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // Envía un correo electrónico utilizando la API REST de Resend
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string message)
        {
            try
            {
                _logger.LogInformation($"Iniciando envío de correo a {toEmail}");

                // Crear el payload de la solicitud con los datos del correo
                var emailRequest = new
                {
                    from = $"{_emailSettings.DisplayName} <{_emailSettings.Mail}>",
                    to = new[] { toEmail },
                    subject = subject,
                    html = message
                };

                // Serializar el payload a formato JSON
                var json = JsonSerializer.Serialize(emailRequest);
                _logger.LogInformation($"Payload: {json}");

                // Configurar la solicitud HTTP con el contenido JSON
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Configurar un timeout de 30 segundos para la solicitud
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                _logger.LogInformation("Enviando solicitud a la API de Resend...");
                var response = await _httpClient.PostAsync("/emails", content, cts.Token);

                // Procesar y registrar la respuesta del servidor
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Código de estado: {(int)response.StatusCode} {response.StatusCode}");
                _logger.LogInformation($"Respuesta: {responseBody}");

                return response.IsSuccessStatusCode;
            }
            catch(TaskCanceledException ex)
            {
                // Manejar errores de timeout
                _logger.LogError($"Timeout al enviar correo: {ex.Message}");
                return false;
            }
            catch(Exception ex)
            {
                // Manejar otros errores que puedan ocurrir durante el envío
                _logger.LogError($"Error al enviar correo: {ex.GetType().Name} - {ex.Message}");
                if(ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }
    }
}