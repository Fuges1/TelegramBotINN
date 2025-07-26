using TelegramBotINN;

internal static class InnCommand
{
    private static bool IsValidInn(string inn)
    {
        if (string.IsNullOrWhiteSpace(inn))
            return false;
        return (inn.Length == 10 || inn.Length == 12) && inn.All(char.IsDigit);
    }

    public static async Task<string> Handle(string messageText, FnsApiService apiService, LastResultCache cache)
    {
        var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            Console.WriteLine("[InnCommand] Ошибка: не указаны ИНН после команды.");
            return "❗ Пожалуйста, укажите один или несколько ИНН через пробел.\nПример: /inn 1234567890 109876543210";
        }

        var inns = parts.Skip(1).Distinct().ToList();
        var results = new List<(string inn, string info)>();

        foreach (var inn in inns)
        {
            if (!IsValidInn(inn))
            {
                Console.WriteLine($"[InnCommand] ❗ Неверный формат ИНН '{inn}'.");
                results.Add((inn, $"❗ ИНН '{inn}' должен содержать только 10 или 12 цифр."));
                continue;
            }

            try
            {
                var result = await apiService.GetInnDataAsync(inn);
                Console.WriteLine($"[InnCommand] Получена информация по ИНН '{inn}': {result}");

                if (result.Contains("(403)") || result.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add((inn, "❗️ Ошибка: доступ к данным запрещён (403). Проверьте правильность API-ключа."));
                }
                else if (result.Contains("Ошибка", StringComparison.OrdinalIgnoreCase) || result.Contains("не найден", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add((inn, $"❗ {result.Trim()}"));
                }
                else
                {
                    var trimmedResult = result.Trim();

                    if (trimmedResult.StartsWith($"🔹 ИНН {inn}"))
                    {
                        trimmedResult = trimmedResult.Substring($"🔹 ИНН {inn}".Length).Trim();
                    }
                    if (trimmedResult.StartsWith("ОБЪЕКТ:"))
                    {
                        trimmedResult = trimmedResult.Substring("ОБЪЕКТ:".Length).Trim();
                    }

                    results.Add((inn, trimmedResult));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InnCommand] ❗ Ошибка при запросе ИНН '{inn}': {ex.Message}");
                results.Add((inn, $"❗ Ошибка при обработке ИНН '{inn}': {ex.Message}"));
            }
        }

        var sortedResults = results
            .OrderBy(r => r.info.StartsWith("❗") ? 1 : 0)
            .Select(r =>
            {
                return $"🔹 ИНН {r.inn}\n{r.info}";
            })
            .ToList();

        var final = string.Join("\n\n", sortedResults);

        cache.Save(final);
        return final;
    }
}
