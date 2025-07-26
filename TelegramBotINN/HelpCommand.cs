namespace TelegramBotINN
{
    internal class HelpCommand
    {
        public static string Handle() =>
        "/start – начать общение\n" +
        "/help – список команд\n" +
        "/hello – контакты разработчика\n" +
        "/inn [ИНН...] – информация о компаниях\n" +
        "/last – повторить последний запрос";
    }
}
