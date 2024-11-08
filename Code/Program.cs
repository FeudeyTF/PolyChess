using LichessAPI.Clients;
using PolyChessTGBot.Bot;
using PolyChessTGBot.Database;
using PolyChessTGBot.Logs;
using PolyChessTGBot.Logs.Types;
using PolyChessTGBot.Managers.Tournaments;
using PolyChessTGBot.Sockets;
using System.Diagnostics;
using System.Reflection;

namespace PolyChessTGBot
{
    public static class Program
    {
        public static readonly DateTime Started;

        public static ConfigFile MainConfig { get; private set; }

        public static readonly TextLog Logger;

        internal static PolyBot Bot;

        internal static PolyData Data;

        internal static readonly SocketServer? Socket;

        internal static LichessClient Lichess;

        internal static DateTime SemesterStartDate;

        internal static TournamentsManager Tournaments;

        static Program()
        {
            Started = DateTime.Now;
            MainConfig = ConfigFile.Load("Main");
            if (DateTime.TryParse(MainConfig.SemesterStartDate, out var date))
                SemesterStartDate = date;
            Logger = new(DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log", MainConfig.LogsFolder);
            string exeFilePath = Path.Combine(
                Environment.CurrentDirectory,
                Assembly.GetExecutingAssembly().GetName().Name + ".exe");
            Logger.Write($"Программа версии {FileVersionInfo.GetVersionInfo(exeFilePath).FileVersion} от {File.GetLastWriteTime(exeFilePath):g}", LogType.Info); 

            Data = new(MainConfig.DatabasePath);
            Data.LoadTables();
            Logger.Write($"База данных '{Data.DatabaseName}' подключена!", LogType.Info);
            Tournaments = new();

            Bot = new(Logger);
            Lichess = new();
            Lichess.OnMessageSent += HandleLichessMessageSent;
            if (MainConfig.Socket.StartSocketServer)
                Socket = new(MainConfig.Socket.Port, Logger);
        }

        public static async Task Main(string[] args)
        {
            #if !DEBUG // Сильно замедляет программу вначале
            await Tournaments.LoadTournaments();
            #endif
            if (string.IsNullOrEmpty(MainConfig.BotToken))
            {
                Logger.Write("Обнаружен пустой токен в конфиге!", LogType.Error);
                Console.ReadLine();
                Environment.Exit(0);
            }
            await Bot.LoadBot();
            Socket?.StartListening();

            while (true)
            {
                var text = Console.ReadLine();
                if (string.IsNullOrEmpty(text))
                    continue;

                if (text.StartsWith('/'))
                    text = text[1..];

                int index = -1;
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == ' ')
                    {
                        index = i;
                        break;
                    }
                }
                string commandName = index < 0 ? text.ToLower() : text[..index].ToLower();
                List<string> parameters = index < 0 ? [] : Utils.ParseParameters(text[index..]);
                switch (commandName.ToLower())
                {
                    case "exit":
                        Environment.Exit(0);
                        break;
                    case "reload":
                        MainConfig = ConfigFile.Load("Main");
                        Console.WriteLine("Перезагрузка конфига прошла успешно!");
                        break;
                    case "send":
                        if (parameters.Count > 0)
                            if (Socket != null)
                                await Socket.SendMessage(string.Join(" ", parameters));
                            else
                                Console.WriteLine("Сокет выключен!");
                        else
                            Console.WriteLine("Неправильный синтаксис! Правильно: /send <message>");
                        break;
                    case "setconfig":
                        if (parameters.Count == 2)
                        {
                            var field = MainConfig.GetType().GetField(parameters[0]);
                            if (field != null)
                            {
                                object? parameter = null;
                                switch(field.FieldType.Name)
                                {
                                    case "Int32":
                                        if (int.TryParse(parameters[1], out int intValue))
                                            parameter = intValue;
                                        else
                                            Console.WriteLine("Неверный тип данных! Тип данных: int");
                                        break;
                                    case "Int64":
                                        if (long.TryParse(parameters[1], out long longValue))
                                            parameter = longValue;
                                        else
                                            Console.WriteLine("Неверный тип данных! Тип данных: long");
                                        break;
                                    case "String":
                                        parameter = parameters[1];
                                        break;
                                }
                                if(parameter == null)
                                {
                                    Console.WriteLine("Тип параметра не поддерживается ввода!");
                                    continue;
                                }

                                field.SetValue(MainConfig, parameter);
                                MainConfig.Save("Main", true);
                                Console.WriteLine($"Параметр '{parameters[0]}' успешно установлен на '{parameters[1]}'!");
                            }
                            else
                                Console.WriteLine($"Параметр '{parameters[0]}' не был найден!");
                        }
                        else
                            Console.WriteLine("Неправильный синтаксис! Правильно: /setconfig <name> <value>");
                        break;
                    default:
                        Console.WriteLine($"Команда '{commandName.ToLower()}' не найдена!");
                        continue;
                }
                Logger.Write($"Была введена команда '{commandName}'. {(parameters.Count > 0 ? $"Аргументы: '{string.Join(", ", parameters)}'" : "Аргументов нет")}", LogType.Info);
            }
        }

        private static void HandleLichessMessageSent(HttpRequestMessage request, HttpResponseMessage response)
        {
            if (MainConfig.ShowLichessApiSending)
                Logger.Write("LICHESS API SEND: " + request.ToString() + " RESPONSE: " + response.ToString(), LogType.Info);
        }
    }
}