using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using TelegramBotINN;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
.ConfigureServices((context, services) =>
{
    var botToken = context.Configuration["BotSettings:Token"];
    var fnsApiKey = context.Configuration["FnsApiSettings:ApiKey"];

    if (string.IsNullOrWhiteSpace(botToken))
        throw new InvalidOperationException("BotToken не задан");

    if (string.IsNullOrWhiteSpace(fnsApiKey))
        throw new InvalidOperationException("FnsApiKey не задан");

    services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));


    services.AddSingleton<FnsApiService>(_ => new FnsApiService(context.Configuration));

    services.AddSingleton<LastResultCache>();
});

var host = builder.Build();

var botClient = host.Services.GetRequiredService<ITelegramBotClient>();
var fnsService = host.Services.GetRequiredService<FnsApiService>();
var cache = host.Services.GetRequiredService<LastResultCache>();

await botClient.DeleteWebhook();

using var cts = new CancellationTokenSource();

bool unauthorizedLogged = false;

var updateHandler = new DefaultUpdateHandler(
    async (bot, update, token) =>
    {
        if (update.Message is null)
        {
            Console.WriteLine("[UpdateHandler] Получено обновление без сообщения.");
            return;
        }

        var message = update.Message;

        if (message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
        {
            var messageText = message.Text!;
            var command = messageText.Split(' ')[0].ToLowerInvariant();

            string response;

            switch (command)
            {
                case "/start":
                    response = StartCommand.Handle();
                    break;

                case "/help":
                    response = HelpCommand.Handle();
                    break;

                case "/hello":
                    response = HelloCommand.Handle();
                    break;

                case "/inn":
                    response = await InnCommand.Handle(messageText, fnsService, cache);
                    break;

                case "/last":
                    response = LastCommand.Handle(cache);
                    break;

                default:
                    response = $"❗ Неизвестная команда '{command}'. Введите /help для списка доступных команд.";
                    Console.WriteLine($"[UpdateHandler] Неизвестная команда: {command}");
                    break;
            }

            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: response,
                cancellationToken: token);
        }
        else
        {
            var messageType = message.Type;

            var messageResponses = new Dictionary<MessageType, string>
            {
                [MessageType.Photo] = "Спасибо за фото, но я могу работать только с текстовыми командами. Введите /help для списка доступных команд.",
                [MessageType.Video] = "Спасибо за видео, но я не умею их обрабатывать. Введите /help для списка доступных команд.",
                [MessageType.Sticker] = "Стикеры — это круто, но, к сожалению, я не понимаю их смысл. Введите /help для списка доступных команд.",
                [MessageType.Voice] = "Голосовые сообщения пока не поддерживаются. Введите /help для списка доступных команд."
            };

            if (!messageResponses.TryGetValue(messageType, out var response))
            {
                response = $"Я получил сообщение типа '{messageType}', но не понимаю, что с ним делать. Введите /help для списка доступных команд.";
            }

            Console.WriteLine($"[UpdateHandler] Получено не текстовое сообщение: {messageType} от пользователя {message.From?.Id}");

            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: response,
                cancellationToken: token);
        }
    },
    (bot, exception, token) =>
    {
        var baseException = exception.GetBaseException();

        if (baseException is Telegram.Bot.Exceptions.ApiRequestException apiEx &&
            apiEx.ErrorCode == 401)
        {
            if (!unauthorizedLogged)
            {
                Console.WriteLine("[UpdateHandler] ❌ Ошибка авторизации: неверный токен Telegram Bot API.");
                unauthorizedLogged = true;
            }
        }
        else
        {
            Console.WriteLine($"[UpdateHandler] ❗ Исключение при обработке: {exception.GetType().Name} — {exception.Message}");
        }

        return Task.CompletedTask;
    }
);

botClient.StartReceiving(
    updateHandler: updateHandler,
    receiverOptions: new ReceiverOptions { AllowedUpdates = [] },
    cancellationToken: CancellationToken.None);

Console.WriteLine("Бот запущен. Нажмите Ctrl+C для остановки.");
await host.RunAsync();