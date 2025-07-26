using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Net;
namespace TelegramBotINN
{
    public class FnsApiService(IConfiguration config)
    {
        private readonly HttpClient _httpClient = new();
        private readonly IConfiguration _config = config;

        public async Task<string> GetInnDataAsync(string inn)
        {
            try
            {
                var requestUrl = $"{_config["FnsApiSettings:ApiUrl"]}/search?q={inn}&key={_config["FnsApiSettings:ApiKey"]}";
                var response = await _httpClient.GetAsync(requestUrl);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("[FnsApiService] ❌ Ошибка авторизации. Проверьте токен API.");
                    return "Ошибка: неверный токен API. Обратитесь к администратору.";
                }

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[FnsApiService] ⚠️ Неудачный ответ от API ({(int)response.StatusCode} {response.StatusCode}).");
                    return $"ИНН {inn}: получен неожиданный ответ от API ({(int)response.StatusCode}).";
                }

                var result = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(result))
                {
                    Console.WriteLine($"[FnsApiService] 🔍 Пустой ответ от API по ИНН: {inn}");
                    return $"ИНН {inn}: не удалось получить данные. Пустой ответ от сервера.";
                }

                try
                {
                    var json = JObject.Parse(result);


                    if (json["items"] == null || !json["items"].HasValues)
                        return $"ИНН {inn}: компания не найдена.";

                    var company = json["items"]![0];
                    var name = company["ЮЛ"]?["НаимПолнЮЛ"]?.ToString() ?? "Название отсутствует";
                    var address = company["ЮЛ"]?["АдресПолн"]?.ToString() ?? "Адрес отсутствует";

                    return $"🔹 ИНН {inn}\n{name}\nАдрес: {address}";
                }
                catch (Exception exJson)
                {
                    Console.WriteLine($"[FnsApiService] Ошибка парсинга JSON: {exJson.Message}");
                    return $"Ошибка при разборе данных для ИНН {inn}.";
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[FnsApiService] 🌐 Ошибка HTTP: {ex.Message}");
                return $"Ошибка соединения с API: {ex.Message}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FnsApiService] ❗ Непредвиденная ошибка: {ex.Message}");
                return $"Непредвиденная ошибка при обработке запроса: {ex.Message}";
            }
        }
    }
}
