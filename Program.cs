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

async Task UpdateRates ()
{ 
var todayrate = await GetRate(client);
//var filePath = ("C:\\Users\\Яна\\Desktop\\RUB.json");
//string todayrate = File.ReadAllText(filePath);
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
 //Dictionary<string, decimal> rates = new Dictionary<string, decimal>();

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
    string messageText1 = "Hello World!";

    string messageText2 = "получислось";

    if (text == "/start")
    {
        await botClient.SendMessage(chatId: chatid, text: messageText1, cancellationToken: ct);
    }
    else if (text == "/info")
    { await botClient.SendMessage(chatId: chatid, text: messageText2, cancellationToken: ct);

    }
    else if (text.StartsWith("/rate_"))
        {
        text = text.Substring(6);
        string[] strings = text.Split('_');
        string from = strings[0].ToUpper();
        string to = strings[1].ToUpper();
        decimal convert = 1 / rates[from];
        DateTime date = DateTime.Now;
        var message =  "Курс валюты на " + date + ":" + "\n"
           + "1" + from + " = " + decimal.Round(convert, 2) + to + "\n"
           + "1" + to + " = " + " " + rates[from] + " " + from ;
        await botClient.SendMessage(chatId: chatid, text: message, cancellationToken: ct);
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
