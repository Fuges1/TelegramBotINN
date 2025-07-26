namespace TelegramBotINN
{
    internal class LastCommand
    {
        public static string Handle(LastResultCache cache) => cache.Get();
    }
}
