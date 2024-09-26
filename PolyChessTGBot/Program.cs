using PolyChessTGBot.Bot;
using PolyChessTGBot.Database;
using PolyChessTGBot.Logs;
using PolyChessTGBot.Logs.Types;

namespace PolyChessTGBot
{
    public static class Program
    {
        public static readonly ConfigFile MainConfig;

        public static readonly TextLog Logger;

        internal static PolyBot Bot;

        internal static PolyData Data;

        static Program()
        {
            MainConfig = ConfigFile.Load("Main");
            Logger = new(DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log", MainConfig.LogsFolder);
            Bot = new(Logger);
            Data = new(MainConfig.DatabasePath);
        }

        public async static Task Main(string[] args)
        {
            if(string.IsNullOrEmpty(MainConfig.BotToken))
            {
                Logger.Write("Обнаружен пустой токен в конфиге!", LogType.Error);
                Console.ReadLine();
                Environment.Exit(0);
            }

            await Bot.LoadBot();

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
                List<string> parameters = index < 0 ? new() : Utils.ParseParameters(text[index..]);
                Logger.Write($"Command {commandName} was entered. Args: '{string.Join(", ", parameters)}'", LogType.Info);
                switch(commandName.ToLower())
                {
                    case "exit":
                        Environment.Exit(0);
                        break;
                }
            }
        }
    }
}