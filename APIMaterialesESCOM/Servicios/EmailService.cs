// Services/EmailService.cs
using APIMaterialesESCOM.Models;
using APIMaterialesESCOM.Servicios;
using Microsoft.Extensions.Options;
using Resend;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace APIMaterialesESCOM.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly HttpClient _httpClient;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("ResendApi");
            _httpClient.BaseAddress = new Uri("https://api.resend.com");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _emailSettings.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string message)
        {
            try
            {
                _logger.LogInformation($"Iniciando envío de correo a {toEmail}");

                // Crear el payload de la solicitud
                var emailRequest = new
                {
                    from = $"{_emailSettings.DisplayName} <{_emailSettings.Mail}>",
                    to = new[] { toEmail },
                    subject = subject,
                    html = message
                };

                // Serializar a JSON
                var json = JsonSerializer.Serialize(emailRequest);
                _logger.LogInformation($"Payload: {json}");

                // Configurar la solicitud HTTP
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Enviando con un timeout mayor (30 segundos)
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                _logger.LogInformation("Enviando solicitud a la API de Resend...");
                var response = await _httpClient.PostAsync("/emails", content, cts.Token);

                // Leer la respuesta
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Código de estado: {(int)response.StatusCode} {response.StatusCode}");
                _logger.LogInformation($"Respuesta: {responseBody}");

                return response.IsSuccessStatusCode;
            }
            catch(TaskCanceledException ex)
            {
                _logger.LogError($"Timeout al enviar correo: {ex.Message}");
                return false;
            }
            catch(Exception ex)
            {
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