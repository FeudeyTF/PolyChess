using PolyChessTGBot.Bot.Commands;
using LichessAPI.Types.Arena;
using LichessAPI.Types.Swiss;
using PolyChessTGBot.Logs;
using System.Reflection;
using File = System.IO.File;
using System.Diagnostics;

namespace PolyChessTGBot.Bot.BotCommands
{
    public partial class BotCommands
    {
        public static readonly string TempPath;

        private static List<ArenaTournamentInfo> TournamentsList;

        private static List<SwissTournamentInfo> SwissTournamentsList;

        static BotCommands()
        {
            TempPath = Path.Combine(Environment.CurrentDirectory, "Temp");
            TournamentsList = [];
            SwissTournamentsList = [];
        }

        public async Task LoadTournaments()
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
                    var tournamentName = Path.GetFileName(filePath)[..^4];
                    if (!Program.MainConfig.UnnecessaryTournaments.Contains(tournamentName))
                    {
                        var tournament = await Program.Lichess.GetTournament(tournamentName);
                        if (tournament != null && tournament.StartDate > date)
                        {
                            var tournamentSheet = await Program.Lichess.GetTournamentSheet(File.OpenText(filePath));

                            if (tournamentSheet != null)
                            {
                                List<string> exclude = new(Program.MainConfig.TopPlayers);
                                tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username) || e.Team != null && !Program.MainConfig.PolytechTeams.Contains(e.Team))).ToList();
                                var tournamentRating = GenerateTournamentRating(tournamentSheet, GetTournamentDivision, GetLichessName, CalculateScore);
                                TournamentsList.Add(new(tournament, tournamentRating));
                            }
                        }
                    }
                }

                foreach (var filePath in Directory.GetFiles(swissPath))
                {
                    var tournamentName = Path.GetFileName(filePath)[..^4];
                    if (!Program.MainConfig.UnnecessaryTournaments.Contains(tournamentName))
                    {
                        var tournament = await Program.Lichess.GetSwissTournament(tournamentName);
                        if (tournament != null && tournament.Started > date)
                        {
                            var tournamentSheet = await Program.Lichess.GetSwissTournamentSheet(File.OpenText(filePath));

                            if (tournamentSheet != null)
                            {
                                List<string> exclude = new(Program.MainConfig.TopPlayers);
                                tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username))).ToList();
                                var tournamentRating = GenerateTournamentRating(tournamentSheet, GetTournamentDivision, GetLichessName, CalculateScore);
                                SwissTournamentsList.Add(new(tournament, tournamentRating));
                            }
                        }
                    }
                }

                TournamentsList = [.. from r in TournamentsList orderby r.Tournament.StartDate descending select r];
                SwissTournamentsList = [.. from r in SwissTournamentsList orderby r.Tournament.Started descending select r];
                Program.Logger.Write($"Найдено {TournamentsList.Count} турниров и {SwissTournamentsList.Count} турниров по швейцарской системе!", LogType.Info);
            }
            else
                Program.Logger.Write("Дата 'TournamentScoresDate' в конфиге не была распознана!", LogType.Error);
        }

        [Command("version", "Отправляет информацию о боте", true)]
        private async Task Version(CommandArgs args)
        {
            string exeFilePath = Path.Combine(
                Environment.CurrentDirectory,
                Assembly.GetExecutingAssembly().GetName().Name + ".exe");
            List<string> message =
            [
                "🛠<b>Информация о боте</b>🛠",
                $"👨🏻‍💻<b>Разработчик:</b> {Program.MainConfig.BotAuthor}",
                $"🔀<b>Версия бота:</b> v.{FileVersionInfo.GetVersionInfo(exeFilePath).FileVersion}",
                $"🕐<b>Дата последнего обновления:</b> {File.GetLastWriteTime(exeFilePath):g}",
                $"⏱<b>Время работы:</b> {DateTime.Now - Program.Started:%d' дн. '%h' ч. '%m' мин. '%s' сек.'}"
            ];
            await args.Reply(string.Join("\n", message));
        }

        private class ArenaTournamentInfo
        {
            public ArenaTournament Tournament;

            public TournamentRating<SheetEntry> Rating;

            public ArenaTournamentInfo(ArenaTournament tournament, TournamentRating<SheetEntry> rating)
            {
                Tournament = tournament;
                Rating = rating;
            }
        }

        private class SwissTournamentInfo
        {
            public SwissTournament Tournament;

            public TournamentRating<SwissSheetEntry> Rating;

            public SwissTournamentInfo(SwissTournament tournament, TournamentRating<SwissSheetEntry> rating)
            {
                Tournament = tournament;
                Rating = rating;
            }
        }
    }
}