using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Lichess.Types.Arena;
using PolyChessTGBot.Lichess.Types.Swiss;
using PolyChessTGBot.Logs;
using System.Reflection;
using File = System.IO.File;

namespace PolyChessTGBot.Bot.BotCommands
{
    public partial class BotCommands
    {
        public static readonly string TempPath;

        private static List<ArenaTournament> TournamentsList;

        private static List<SwissTournament> SwissTournamentsList;

        static BotCommands()
        {
            TempPath = Path.Combine(Environment.CurrentDirectory, "Temp");
            TournamentsList = [];
            SwissTournamentsList = [];
        }

        public static async Task LoadTournaments()
        {
            if (DateTime.TryParse(Program.MainConfig.TournamentScoresDate, out var date))
            {
                Program.Logger.Write($"Собираем турниры от {date:g}...", LogType.Info);
                var tournamentsPath = Path.Combine(Environment.CurrentDirectory, "Tournaments");
                if (!Directory.Exists(tournamentsPath))
                    Directory.CreateDirectory(tournamentsPath);

                var swissPath = Path.Combine(Environment.CurrentDirectory, "SwissTournaments");
                if (!Directory.Exists(swissPath))
                    Directory.CreateDirectory(swissPath);

                foreach (var filePath in Directory.GetFiles(tournamentsPath))
                {
                    var tournament = await Program.Lichess.GetTournament(Path.GetFileName(filePath)[..^4]);
                    if (tournament != null && tournament.StartDate > date)
                        TournamentsList.Add(tournament);
                }

                foreach (var filePath in Directory.GetFiles(swissPath))
                {
                    var tournament = await Program.Lichess.GetSwissTournament(Path.GetFileName(filePath)[..^4]);
                    if (tournament != null && tournament.Started > date)
                        SwissTournamentsList.Add(tournament);
                }
                TournamentsList = [.. from r in TournamentsList orderby r.StartDate descending select r];
                SwissTournamentsList = [.. from r in SwissTournamentsList orderby r.Started descending select r];
                Program.Logger.Write($"Найдено {TournamentsList.Count} турниров и {SwissTournamentsList.Count} турниров по швейцарской системе!", LogType.Info);
            }
            else
                Program.Logger.Write("Дата 'TournamentScoresDate' в конфиге не была распознана!", LogType.Error);
        }

        [Command("version", "Отправляет информацию о боте", true)]
        public async Task Version(CommandArgs args)
        {
            string exeFilePath = Path.Combine(
                Environment.CurrentDirectory,
                Assembly.GetExecutingAssembly().GetName().Name + ".exe");
            List<string> message =
            [
                "🛠<b>Информация о боте</b>🛠",
                $"👨🏻‍💻<b>Разработчик:</b> {Program.MainConfig.BotAuthor}",
                $"🔀<b>Версия бота:</b> v.{Program.Version}",
                $"🕐<b>Дата последнего обновления:</b> {File.GetLastWriteTime(exeFilePath):g}",
                $"⏱<b>Время работы:</b> {DateTime.Now - Program.Started:%d' дн. '%h' ч. '%m' мин. '%s' сек.'}"
            ];
            await args.Reply(string.Join("\n", message));
        }
        private struct User(long telegramID, string name, string lichessName, long year)
        {
            public long TelegramID = telegramID;

            public string Name = name;

            public string LichessName = lichessName;

            public long Year = year;

            public override readonly string ToString()
            {
                return $"{Name} '{LichessName}' ({TelegramID}), Курс - {Year}";
            }
        }
    }
}