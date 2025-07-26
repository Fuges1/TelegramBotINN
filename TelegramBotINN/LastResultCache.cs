namespace TelegramBotINN
{
    internal class LastResultCache
    {
        private string? _lastResponse;

        public void Save(string response)
        {
            _lastResponse = response;
        }

        public string Get() => _lastResponse ?? "Нет предыдущего запроса.";
    }
}
