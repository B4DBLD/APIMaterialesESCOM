using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace APIMaterialesESCOM.Models
{
    public class ApiRequest
    {
        public readonly string url = "http://158.23.160.166:8081/repositorio/Autores";
        private static readonly HttpClient client = new HttpClient();
        public async Task<ApiResponse> CrearAutor(Autor autor)
        {
            ApiResponse apiResponse = new ApiResponse();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                string jsonPayload = JsonSerializer.Serialize(autor);
                HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseBody))
                {
                    try
                    {
                         apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseBody, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                         // Devuelve el objeto con el valor de "Ok"
                    }
                    catch (JsonException jsonEx)
                    {
                        // Si el cuerpo no es un JSON válido o no contiene "Ok"
                        Console.WriteLine($"Error al deserializar la respuesta JSON: {jsonEx.Message}");
                        Console.WriteLine($"Cuerpo de la respuesta: {responseBody}");
                        // Devolvemos un objeto indicando que no fue Ok, ya que no pudimos parsearlo
                        apiResponse = new ApiResponse { Ok = false };
                    }
                }
                return apiResponse;
            }
            catch (Exception ex)
            {
                return new ApiResponse { Ok = false};
            }
        }

        public async Task<ApiResponse> ObtenerAutor(string email)
        {
            string urlGet = $"{url}/email/{Uri.EscapeDataString(email)}"; // Asumiendo que el email es único y se usa para identificar al autor
            ApiResponse apiResponse = new ApiResponse { Ok = false };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                HttpResponseMessage response = await client.GetAsync(urlGet);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseBody))
                {
                    try
                    {
                        apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseBody, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        // Devuelve el objeto con el valor de "Ok"
                    }
                    catch (JsonException jsonEx)
                    {
                        // Si el cuerpo no es un JSON válido o no contiene "Ok"
                        Console.WriteLine($"Error al deserializar la respuesta JSON: {jsonEx.Message}");
                        Console.WriteLine($"Cuerpo de la respuesta: {responseBody}");
                        // Devolvemos un objeto indicando que no fue Ok, ya que no pudimos parsearlo
                        apiResponse = new ApiResponse { Ok = false };
                    }
                }
                return apiResponse;
            }
            catch (Exception ex)
            {
                return new ApiResponse { Ok = false };
            }


        }

        public async Task<ApiResponse> CrearRelacion(int userID, int autorID)
        {
            string urlGet = $"{url}/CreateRelacion/{userID}?autorID={autorID}"; // Asumiendo que el email es único y se usa para identificar al autor
            ApiResponse apiResponse = new ApiResponse { Ok = false };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                HttpResponseMessage response = await client.GetAsync(urlGet);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseBody))
                {
                    try
                    {
                        apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseBody, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        // Devuelve el objeto con el valor de "Ok"
                    }
                    catch (JsonException jsonEx)
                    {
                        // Si el cuerpo no es un JSON válido o no contiene "Ok"
                        Console.WriteLine($"Error al deserializar la respuesta JSON: {jsonEx.Message}");
                        Console.WriteLine($"Cuerpo de la respuesta: {responseBody}");
                        // Devolvemos un objeto indicando que no fue Ok, ya que no pudimos parsearlo
                        apiResponse = new ApiResponse { Ok = false };
                    }
                }
                return apiResponse;
            }
            catch (Exception ex)
            {
                return new ApiResponse { Ok = false };
            }
        }

        public async Task<ApiResponse> GetRelacion(int userID)
        {
            string urlGet = $"{url}/GetRelacion/{userID}"; // Asumiendo que el email es único y se usa para identificar al autor
            ApiResponse apiResponse = new ApiResponse { Ok = false };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                HttpResponseMessage response = await client.GetAsync(urlGet);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseBody))
                {
                    try
                    {
                        apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseBody, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        // Devuelve el objeto con el valor de "Ok"
                    }
                    catch (JsonException jsonEx)
                    {
                        // Si el cuerpo no es un JSON válido o no contiene "Ok"
                        Console.WriteLine($"Error al deserializar la respuesta JSON: {jsonEx.Message}");
                        Console.WriteLine($"Cuerpo de la respuesta: {responseBody}");
                        // Devolvemos un objeto indicando que no fue Ok, ya que no pudimos parsearlo
                        apiResponse = new ApiResponse { Ok = false };
                    }
                }
                return apiResponse;
            }
            catch (Exception ex)
            {
                return new ApiResponse { Ok = false };
            }
        }

    }
}
