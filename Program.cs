using System;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks; // Для асинхронности
using Telegram.Bot;
using Telegram.Bot.Polling; // Добавляем пространство имён для ReceiverOptions
using Telegram.Bot.Types;
using static System.Net.Mime.MediaTypeNames;
Dictionary<string, decimal> rates = new Dictionary<string, decimal>();
HttpClient client = new HttpClient();
UpdateRates();
static async Task<string?> GetRate(HttpClient client)

{
    string url = "https://v6.exchangerate-api.com/v6/db382fbf8c61ddb61bb2b597/latest/RUB";
    var response = await client.GetAsync(url);
    string answer = await response.Content.ReadAsStringAsync();

    return answer;
}

async Task UpdateRates()
{
    var todayrate = await GetRate(client);
    Console.WriteLine("данные прочтены");
    JsonDocument json = JsonDocument.Parse(todayrate);//todayrate - вернуть потом для API 
    JsonElement root = json.RootElement;
    string result = root.GetProperty("result").ToString();
    if (result != "success")
    {
        Console.WriteLine("Ошибка API");
        return;
    }
    //ПОлучение с-ва "conversion_rates" из всего объекта и его элементов 
    JsonElement conversion_rates = root.GetProperty("conversion_rates");

    foreach (JsonProperty rate in conversion_rates.EnumerateObject())
    {
        string currency = rate.Name;
        decimal value = rate.Value.GetDecimal();
        rates[currency] = value;
    }
}
async Task everydayupdate()
{
    while (true)
    {
        await UpdateRates();
        await Task.Delay(TimeSpan.FromHours(24));
    }
}

TelegramBotClient botClient = new TelegramBotClient("8208641775:AAFWNrFLE4OO_PZDJbtvMyN7jjCLdXp_pjQ");

// Изменяем сигнатуру на ту, что ожидает StartReceiving
async Task HandleUpdate(ITelegramBotClient botClient, Update update, CancellationToken ct)
{
    if (update.Message == null)
        return;

    if (update.Message.Text == null)
        return;

    string text = update.Message.Text;
    long chatid = update.Message.Chat.Id;
    string messageText2 = "Я могу показывать актуальные курсы валют к рублю. " +
    " \r\n\r\nПример использования: " +
    " \r\n/rate_usd_rub — курс доллара к рублю и обратно " +
    " \r\n/rate_eur_rub — курс евро к рублю и обратно  " +
    "\r\n\r\nКурсы обновляются автоматически каждые 24 часа, так что информация всегда свежая. " +
    " \r\n\r\n⚠️ Вводи команды точно в нижнем регистре, например: /rate_usd_rub";

    string messageText1 = "Привет! 👋  \r\n" +
        "Я — ExRater_bot, твой помощник по курсам валют." +
        "  \r\n\r\nС моей помощью ты можешь:  " +
        "\r\n💱 Узнать актуальный курс валют к рублю  " +
        "\r\n📊 Получить обратный курс (рубль к выбранной валюте) " +
        " \r\n\r\nИспользуй команды: " +
        " \r\n/start — показать это сообщение  " +
        "\r\n/info — узнать о возможностях бота  " +
        "\r\n/rate_usd_rub — пример команды для курса USD → RUB  " +
        "\r\n\r\nДавай начнём! 😉";

    if (text == "/start")
    {
        await botClient.SendMessage(chatId: chatid, text: messageText1, cancellationToken: ct);
    }
    else if (text == "/info")
    {
        await botClient.SendMessage(chatId: chatid, text: messageText2, cancellationToken: ct);

    }
    else if (text.StartsWith("/rate_"))
    {
        text = text.Substring(6);
        string[] strings = text.Split('_');
        string from = strings[0].ToUpper();
        string to = strings[1].ToUpper();
        decimal convert = 1 / rates[from];
        decimal convertTest = 0.01m;
        DateTime date = DateTime.Now;
        if (convert < convertTest)
        {
            var message = "Курс валюты на " + date + ":" + "\n"
          + "1" + to + " = " + " " + rates[from] + " " + from;
         await botClient.SendMessage(chatId: chatid, text: message, cancellationToken: ct);
        }
        else
        {
            var message = "Курс валюты на " + date + ":" + "\n"
               + "1" + from + " = " + decimal.Round(convert, 2) + to + "\n"
               + "1" + to + " = " + " " + rates[from] + " " + from;
         await botClient.SendMessage(chatId: chatid, text: message, cancellationToken: ct);
        }
        
    }

}

// Функция для обработки возможных ошибок при получении обновлений
Task HandlePollingError(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken ct)
{
    // Выводим ошибку в консоль
    Console.WriteLine($"Ошибка при получении обновлений: {exception.Message}");
    return Task.CompletedTask;
}

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Используем ReceiverOptions и передаём его как четвёртый параметр
botClient.StartReceiving(
   HandleUpdate,
   HandlePollingError,
   new ReceiverOptions(), // Настройки приёма
   cts.Token // Токен отмены
);

Console.WriteLine("Бот запущен. Нажмите Ctrl+C для завершения...");
await Task.Delay(Timeout.Infinite, cts.Token); // Ждём бесконечно, пока не придёт сигнал отмены


