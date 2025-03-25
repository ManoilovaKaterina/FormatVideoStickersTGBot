using Microsoft.VisualBasic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using FFMpegCore;

class Program
{
    private static TelegramBotClient Client;
    private static ConcurrentDictionary<long, string> _userFileRequests = new ConcurrentDictionary<long, string>();
    private static CancellationTokenSource _cts = new CancellationTokenSource();

    static void CropVideo(string inputFilePath, string outputFilePath)
    {
        string scaleFilter = "scale=-1:512";
        string cropFilter = "crop=512:512:0:512";
        FFMpegArguments
            .FromFileInput(inputFilePath)
            .OutputToFile(outputFilePath, true, options => options
                .WithCustomArgument($"-vf \"{scaleFilter},{cropFilter}\"")
                .WithCustomArgument($"-t 3")
                .WithCustomArgument("-c:v libvpx-vp9 -b:v 400k -an")
                .WithFastStart())
            .ProcessSynchronously();
    }
    static async Task Main(string[] args)
    {
        var botToken = Environment.GetEnvironmentVariable("VIDEOSTICKERS_BOT_TOKEN");
        Client = new TelegramBotClient(botToken, cancellationToken: _cts.Token);
        var me = await Client.GetMe();

        Console.WriteLine($"@{me.Username} працює... вимкнути на ентер");

        await SetBotCommandsAsync();

        Client.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            },
            cancellationToken: _cts.Token
        );

        Console.ReadLine();
        _cts.Cancel();
    }

    private static async Task SetBotCommandsAsync()
    {
        await Client.DeleteMyCommands();

        var commands = new List<BotCommand>
        {
            new BotCommand { Command = "start", Description = "Старт" }
        };

        await Client.SetMyCommands(commands);
        Console.WriteLine("Команди виставлені");
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message is { } message)
        {
            await OnMessage(message);
        }
        else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery is { } callbackQuery)
        {
            await OnUpdate(callbackQuery);
        }
    }

    private static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(exception);
    }

    private static async Task OnMessage(Message msg)
    {
        if (msg.Text == "/start")
        {
            await Client.SendMessage(msg.Chat.Id, "Привіт! Я - бот форматувальщик відео для стикерів тг\n" +
                "Використання: надішли мені відео, яке хочеш зробити стікером, і я відформатую файл як треба, а потім просто перешли його в стікербот");
        }
        else if (msg.Type == MessageType.Video || msg.Type == MessageType.VideoNote)
        {
            var fileId = msg.Type == MessageType.Video ? msg.Video.FileId : msg.VideoNote.FileId;
            try
            {
                await Client.SendMessage(msg.Chat.Id, "Дякую! Триває обробка файлу...");
                var fileName = msg.From.Id;
                var file = await Client.GetFile(fileId);

                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{fileName}.mp4");
                var outFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{fileName}out.webm");

                using (var saveStream = new FileStream(filePath, FileMode.Create))
                {
                    await Client.DownloadFile(file.FilePath, saveStream);
                    CropVideo(filePath, outFile);
                    await SendResultMessage(msg.Chat.Id, filePath, outFile);
                }
                System.IO.File.Delete(filePath);
                System.IO.File.Delete(outFile);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await Client.SendMessage(msg.Chat.Id, "Виникла помилка :(");
            }
            await Client.SendMessage(msg.Chat.Id, "Готово! Надішліть наступний файл");
        }
    }

    private static async Task SendResultMessage(long chatId, string inFile, string file)
    {
        using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            await Client.SendVideo(chatId, new InputFileStream(stream, Path.GetFileName(file)));
        }
    }

    private static async Task OnUpdate(CallbackQuery query)
    {
        await Client.AnswerCallbackQuery(query.Id, $"Ви обрали {query.Data}");
        await Client.SendMessage(query.Message.Chat.Id, $"Юзер {query.From.Username} клікнув на {query.Data}");
    }
}
