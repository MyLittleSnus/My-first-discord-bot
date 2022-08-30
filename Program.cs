using System.Globalization;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using BotConfiguration.Context;
using BotConfiguration.Entities;
using Bot.Rebooting;

DiscordSocketClient client = null;
BotDbContext botDatabase = null;

bool needsLogging = false;
string CSVContent = "";
string token = "";
ulong channelID = 0;

IConfiguration configurationBuilder = new ConfigurationBuilder()
        .AddJsonFile("/Users/mihailtkacenko/Projects/MyFirstBot/appsettings.json", optional: true, reloadOnChange: true)
        .Build();

var recovery = new BotRecovery(configurationBuilder);
var botRecovery = recovery.Load();
botRecovery.ConfigBuilder = configurationBuilder;

await LaunchBot();

void Configure()
{
    var dbPath = configurationBuilder.GetConnectionString("DefaultConnection");

    channelID = ulong.Parse(configurationBuilder
        .GetSection("BotSettings")
        .GetSection("ResponseChannelId")
        .Value);
    
    var filePath = configurationBuilder
        .GetSection("BirthdayFile")
        .GetSection("CSV")
        .Value;

    token = configurationBuilder
        .GetSection("BotSettings")
        .GetSection("Token")
        .Value;

    var options = new DbContextOptionsBuilder().UseSqlite(dbPath).Options;
    botDatabase = new(options);

    CSVContent = File.ReadAllText(filePath);
}

List<Participant> ParseCSV(string content)
{
    var splitter = content.Contains("\r\n") ? "\r\n" : "\n";
    var lines = content.Split(splitter);
    var participants = new List<Participant>();
    
    foreach(var line in lines)
    {
        var data = line.Split(",");
        var personCredits = data[0].Split(" ");
        
        var participant = new Participant();

        participant.Name = personCredits[0];
        participant.Surname = personCredits[1];
        participant.BirthdayDate = !string.IsNullOrWhiteSpace(data[1]) ? DateTime.ParseExact(data[1] + " 00:00:00 AM", "dd.M.yyyy hh:mm:ss tt", CultureInfo.InvariantCulture) : null;

        participants.Add(participant);
    }

    return participants;
}

void BotStartUp()
{
    Configure();

    botDatabase.Database.EnsureCreated();

    var participants = ParseCSV(CSVContent);
    
    botDatabase.Participants.AddRange(ParseCSV(CSVContent));
    botDatabase.SaveChanges();
}

async Task LaunchBot()
{
    client = new();

    command:  var command = Console.ReadLine();

    switch (command)
    {
        case "configure_bot": Configure(); goto command;
        case "logging_enabled": needsLogging = true; goto command;
        case "logging_disabled": needsLogging = false; goto command;
        case "connect": break;
        default: goto command;
    }

    if (needsLogging)
        client.Log += (LogMessage msg) => { Console.WriteLine(msg); return Task.CompletedTask; };

    client.MessageReceived += HandleCommands;

    await client.LoginAsync(TokenType.Bot, token);
    await client.StartAsync();

    client.Ready += Process;

    Console.ReadKey();
}

Task Process()
{
    Thread thread = new Thread(async () =>
    {
        while (true)
        {
            if (DateTime.Now.Day == botRecovery.NextInvokeCheckBirthday.Day)
            {
                await CheckBirthdays();
                botRecovery.NextInvokeCheckBirthday = botRecovery.NextInvokeCheckBirthday.AddDays(1);
                botRecovery.Save();
            }

            await Task.Delay(60000);
        }
    });

    thread.Start();

    return Task.CompletedTask;
}

Task HandleCommands(SocketMessage message)
{
    if (!message.Author.IsBot)
    {
        var parameters = message.Content.Split(" ");

        switch (parameters[0])
        {
            case "!добавить":
                var reply = CheckNewPersonWithReply(message, parameters);
                message.Channel.SendMessageAsync(reply);
                break;
        }
    }

    return Task.CompletedTask;
}

string CheckNewPersonWithReply(SocketMessage message, params string[] inputs)
{
    if (inputs.Length - 1 < 4)
        return $"{message.Author.Username}, за дурака меня держишь?";

    var isNameCorrect = string.IsNullOrWhiteSpace(inputs[1]);
    var isSurnameCorrect = string.IsNullOrWhiteSpace(inputs[2]);
    var isUsernameCorrect = string.IsNullOrWhiteSpace(inputs[3]);
    var isDateTimeCorrect = DateTime.TryParse(inputs[3], out DateTime date);

    string reply = "";

    var newParticipant = new Participant();
    if (!isNameCorrect)
        reply += $"Бле, чел, имя {inputs[1]} херня какая-то, давай по новой\n";
    else newParticipant.Name = inputs[1];
    if (!isSurnameCorrect)
        reply += $"фамилия {inputs[2]} – ерунда";
    else newParticipant.Surname = inputs[2];
    if (!isUsernameCorrect)
        reply += $"возможно никнейм ты написал с собачкой, убери по хорошему\n";
    else newParticipant.Username = inputs[3];
    if (!isDateTimeCorrect)
        reply += $"формат даты днюхи неверный, правильный формат: ГГГГ-MM-ДД\n";
    else
    {
        var components = inputs[4].Split("-");
        newParticipant.BirthdayDate = new DateTime(int.Parse(components[0]), int.Parse(components[1]), int.Parse(components[2]));
    }

    return reply == "" ? "Окс, добавлен" : reply;
}

async Task CheckBirthdays()
{
    var participants = botDatabase.Participants;

    foreach (var participant in participants)
    {
        if (participant.BirthdayDate != null &&
            participant.BirthdayDate.Value.Day == DateTime.Now.Day &&
            participant.BirthdayDate.Value.Month == DateTime.Now.Month)
        {
            var channel = client.GetChannel(channelID) as SocketTextChannel;
            var timeSpan = DateTime.Now.Subtract(participant.BirthdayDate.Value);
            var age = (int)Math.Floor(timeSpan.TotalDays / 365);
            var user = channel.Users.Where(x => x.Username == participant.Username).FirstOrDefault();
            await channel.SendMessageAsync($"{user.Mention} С Днюхой  {participant.Name} {participant.Surname}! Сегодня тебе исполняется aж {age} лет!");
        }
    }
}