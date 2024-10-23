using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Extensions;
using LichessAPI.Types.Arena;
using LichessAPI.Types.Swiss;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChessTGBot.Bot.BotCommands
{
    public partial class BotCommands
    {
        private Division DivisionC = new(0, 1300);

        private Division DivisionB = new(1301, 1800);

        private Division DivisionA = new(1801, 2100);

        [Command("getplayerscore", "Показывает результат участие ученика в турнире", admin: true)]
        private async Task GetPlayerScore(CommandArgs args)
        {
            if (args.Parameters.Count == 2)
            {
                var tournamentId = args.Parameters[0];
                var name = args.Parameters[1];

                var tournament = await Program.Lichess.GetTournament(tournamentId);

                var directory = Path.Combine(Environment.CurrentDirectory, "Tournaments");
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                var filePath = Path.Combine(directory, tournamentId + ".txt");
                if (File.Exists(filePath))
                {
                    var tournamentSheet = await Program.Lichess.GetTournamentSheet(File.OpenText(filePath));
                    if (tournament != null && tournamentSheet != null)
                    {
                        using var reader = Program.Data.SelectQuery($"SELECT * FROM Users WHERE Name='{name}'");
                        LichessAPI.Types.User? lichessUser = null;
                        if (reader.Read())
                        {
                            lichessUser = await Program.Lichess.GetUserAsync(reader.Get("LichessName"));
                        }
                        else
                            lichessUser = await Program.Lichess.GetUserAsync(name);
                        if (lichessUser != null)
                        {
                            foreach (var player in tournamentSheet)
                            {
                                if (player.Sheet != null && player.Username == lichessUser.Username)
                                {
                                    List<string> text = [
                                        $"Турнир <b>{tournament.FullName}</b>. Состоялся <b>{tournament.StartDate:g}</b>",
                                        $"Информация об участнике турнира <b>{player.Username}</b>:",
                                        $"<b>Ранг:</b> {player.Rank}",
                                        $"<b>Набрано очков:</b> {player.Score}",
                                        $"<b>Итоговая строка:</b> {player.Sheet.Scores}",
                                        ];

                                    if (player.Team != null)
                                    {
                                        var team = await Program.Lichess.GetTeamAsync(player.Team);
                                        if (team != null)
                                            text.Add($"<b>Команда:</b> {team.Name}");
                                    }

                                    int zeroNumbers = player.Sheet.Scores.Count(c => c == '0');
                                    int twoNumbers = player.Sheet.Scores.Count(c => c == '2');
                                    int fourNumbers = player.Sheet.Scores.Count(c => c == '4');
                                    int total = zeroNumbers + twoNumbers + fourNumbers;
                                    if (total >= 7 && twoNumbers >= 1)
                                    {
                                        text.Add("По критериям регламента игрок <b>участвовал</b> в турнире");
                                    }
                                    else
                                    {
                                        text.Add("По критериям регламента игрок <b>не участвовал</b> в турнире");
                                    }
                                    TelegramMessageBuilder message = string.Join('\n', text);
                                    InlineKeyboardButton playerInfo = new($"🔍Информация об игроке {lichessUser.Username}");
                                    playerInfo.SetData("UserInfo", ("Name", lichessUser.Username));
                                    message.AddButton(playerInfo);
                                    await args.Reply(message);
                                    return;
                                }
                            }
                            await args.Reply($"Ученик {name} не участовал в турнире");
                        }
                        else
                            await args.Reply("Аккаунт Lichess не найден!");
                    }
                    else
                        await args.Reply("Турнир не был найден!");
                }
                else
                    await args.Reply("Турнир не сохранён с помощью команды /savearena!");
            }
            else
                await args.Reply("Неправильный синтаксис! Правильно: /getplayerscore \"ID турнира\" \"Ник ученика\"");
        }

        [Command("savearena", "Сохраняет арену", admin: true)]
        private async Task SaveArena(CommandArgs args)
        {
            if (args.Parameters.Count == 1)
            {
                var tournamentId = args.Parameters[0];
                var tournament = await Program.Lichess.GetTournament(tournamentId);
                if (tournament != null)
                {
                    var directory = Path.Combine(Environment.CurrentDirectory, "Tournaments");
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    await Program.Lichess.SaveTournamentSheet(Path.Combine(directory, tournamentId + ".txt"), tournamentId, true);
                    await args.Reply($"Турнир <b>{tournament.FullName}</b> был сохранён!");
                }
                else
                    await args.Reply("Турнир не был найден!");
            }
            else
                await args.Reply("Неправильный синтаксис! Правильно: /savearena \"ID турнира\"");
        }

        [Command("saveswiss", "Сохраняет турнир по швейцарской системе", admin: true)]
        private async Task SaveSwissTournament(CommandArgs args)
        {
            if (args.Parameters.Count == 1)
            {
                var tournamentId = args.Parameters[0];
                var tournament = await Program.Lichess.GetSwissTournament(tournamentId);
                if (tournament != null)
                {
                    var directory = Path.Combine(Environment.CurrentDirectory, "SwissTournaments");
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    await Program.Lichess.SaveSwissTournamentSheet(Path.Combine(directory, tournamentId + ".txt"), tournamentId);
                    await args.Reply($"Турнир <b>{tournament.Name}</b> был сохранён!");
                }
                else
                    await args.Reply("Турнир не был найден!");
            }
            else
                await args.Reply("Неправильный синтаксис! Правильно: /saveswiss \"ID турнира\"");
        }

        [Command("swissresult", "Генерирует таблицу с результатами участников турнира по швейцарской системе", admin: true)]
        private async Task GenerateSwissTournamentTable(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                var tournamentId = args.Parameters[0];
                var tournament = await Program.Lichess.GetSwissTournament(tournamentId);

                var directory = Path.Combine(Environment.CurrentDirectory, "SwissTournaments");
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                var filePath = Path.Combine(directory, tournamentId + ".txt");
                if (File.Exists(filePath))
                {
                    var tournamentSheet = await Program.Lichess.GetSwissTournamentSheet(File.OpenText(filePath));
                    List<string> exclude = new(Program.MainConfig.TopPlayers);
                    if (args.Parameters.Count > 1)
                    {
                        var stringsToExclude = args.Parameters[1..].Select(p => p.Split(','));
                        foreach (var str in stringsToExclude)
                            foreach (var str2 in str)
                                if (!string.IsNullOrEmpty(str2.Trim()))
                                    exclude.Add(str2.Trim());
                    }
                    tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username))).ToList();
                    if (tournament != null && tournamentSheet != null)
                    {
                        List<string> csv = ["Имя;Ник Lichess;Балл"];
                        List<string> text = [
                            $"Турнир по швейцарской <b>{tournament.Name}</b>. Состоялся <b>{tournament.Started:g}</b>",
                            $"Информация об участии в турнире"
                        ];

                        var tournamentRating = GenerateTournamentRating(tournamentSheet, GetTournamentDivision, GetLichessName, CalculateScore);

                        foreach (var division in tournamentRating.Divisions)
                        {
                            text.Add($"Игроки дивизиона <b>{division.Key}</b>:");
                            foreach (var entry in division.Value)
                                text.Add($"<b> - {entry.Rank}) {entry.Username}</b>. Рейтинг: {entry.Rating}");
                        }

                        text.Add("");
                        text.Add("<b>Остальной рейтинг и баллы за турнир:</b>");
                        text.Add("");

                        foreach (var entry in tournamentRating.Players)
                        {
                            if (entry.Score != -1)
                                csv.Add($"{entry.User?.Name};{entry.TournamentEntry.Username};{entry.Score}");
                            text.Add($"<b>{entry.TournamentEntry.Rank}) {entry.TournamentEntry.Username}</b>, {(string.IsNullOrEmpty(entry.User?.Name) ? "Без имени" : entry.User?.Name)}. Балл: {(entry.Score == -1 ? "-" : entry.Score)}");
                        }

                        TelegramMessageBuilder message = "Файл с таблицей результатов";
                        if (!Directory.Exists(TempPath))
                            Directory.CreateDirectory(TempPath);
                        var csvFilePath = Path.Combine(TempPath, tournament.ID + "result.csv");
                        if (File.Exists(csvFilePath))
                            File.Delete(csvFilePath);
                        using (var streamWriter = new StreamWriter(File.Create(csvFilePath), Encoding.UTF8))
                        {
                            foreach (var entry in csv)
                                streamWriter.WriteLine(entry);
                            streamWriter.Close();
                        }
                        using var stream = File.Open(csvFilePath, FileMode.Open);
                        message.WithFile(stream, "Table.csv");
                        await args.Reply(string.Join('\n', text));
                        await args.Reply(message);
                    }
                    else
                        await args.Reply("Турнир не был найден!");
                }
                else
                    await args.Reply("Турнир не сохранён с помощью команды /saveswiss!");
            }
            else
                await args.Reply("Неправильный синтаксис! Правильно: /swissresult \"ID турнира\"");
        }

        [Command("arenaresult", "Генерирует таблицу с результатами участников арены", admin: true)]
        private async Task GenerateTournamentTable(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                var tournamentId = args.Parameters[0];
                var tournament = await Program.Lichess.GetTournament(tournamentId);

                var directory = Path.Combine(Environment.CurrentDirectory, "Tournaments");
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                var filePath = Path.Combine(directory, tournamentId + ".txt");
                if (File.Exists(filePath))
                {
                    var tournamentSheet = await Program.Lichess.GetTournamentSheet(File.OpenText(filePath));
                    List<string> exclude = new(Program.MainConfig.TopPlayers);
                    if (args.Parameters.Count > 1)
                    {
                        var stringsToExclude = args.Parameters[1..].Select(p => p.Split(','));
                        foreach (var str in stringsToExclude)
                            foreach (var str2 in str)
                                if (!string.IsNullOrEmpty(str2.Trim()))
                                    exclude.Add(str2.Trim());
                    }

                    if (tournament != null && tournamentSheet != null)
                    {
                        tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username) || e.Team != null && !Program.MainConfig.PolytechTeams.Contains(e.Team))).ToList();
                        List<string> csv = ["Имя;Ник Lichess;Балл"];
                        List<string> text = [
                            $"Турнир <b>{tournament.FullName}</b>. Состоялся <b>{tournament.StartDate:g}</b>",
                            $"Информация об участии в турнире"
                        ];

                        var tournamentRating = GenerateTournamentRating(tournamentSheet, GetTournamentDivision, GetLichessName, CalculateScore);

                        foreach (var divison in tournamentRating.Divisions)
                        {
                            text.Add($"Игроки дивизиона <b>{divison.Key}</b>:");
                            foreach (var entry in divison.Value)
                                text.Add($"<b> - {entry.Rank}) {entry.Username}</b>. Рейтинг: {entry.Rating}");
                        }

                        text.Add("");
                        text.Add("<b>Остальной рейтинг и баллы за турнир:</b>");
                        text.Add("");

                        foreach (var entry in tournamentRating.Players)
                        {
                            if (entry.Score != -1)
                                csv.Add($"{entry.User?.Name};{entry.TournamentEntry.Username};{entry.Score}");
                            text.Add($"<b>{entry.TournamentEntry.Rating}) {entry.TournamentEntry.Username}</b>, {(string.IsNullOrEmpty(entry.User?.Name) ? "Без имени" : entry.User?.Name)}. Балл: {(entry.Score == -1 ? "-" : entry.Score)}");
                        }

                        TelegramMessageBuilder message = "Файл с таблицей результатов";
                        if (!Directory.Exists(TempPath))
                            Directory.CreateDirectory(TempPath);
                        var csvFilePath = Path.Combine(TempPath, tournament.ID + "result.csv");
                        if (File.Exists(csvFilePath))
                            File.Delete(csvFilePath);
                        using (var streamWriter = new StreamWriter(File.Create(csvFilePath), Encoding.UTF8))
                        {
                            foreach (var entry in csv)
                                streamWriter.WriteLine(entry);
                            streamWriter.Close();
                        }
                        using var stream = File.Open(csvFilePath, FileMode.Open);
                        message.WithFile(stream, "Table.csv");
                        await args.Reply(string.Join('\n', text));
                        await args.Reply(message);
                    }
                    else
                        await args.Reply("Турнир не был найден!");
                }
                else
                    await args.Reply("Турнир не сохранён с помощью команды /savearena!");
            }
            else
                await args.Reply("Неправильный синтаксис! Правильно: /arenaresult \"ID турнира\"");
        }

        private int CalculateScore(SwissSheetEntry entry, bool inDivision)
        {
            int totalScore = -1;
            if (!entry.Absent)
            {
                totalScore = 0;
                if (inDivision)
                    totalScore = 1;
            }
            return totalScore;
        }

        private int CalculateScore(SheetEntry entry, bool inDivision)
        {
            int totalScore = -1;
            if (entry.Sheet != null)
            {
                int zeroNumbers = entry.Sheet.Scores.Count(c => c == '0');
                int twoNumbers = entry.Sheet.Scores.Count(c => c == '2');
                int fourNumbers = entry.Sheet.Scores.Count(c => c == '4');
                int total = zeroNumbers + twoNumbers + fourNumbers;

                if (inDivision)
                    totalScore = 1;
                else if (total >= 7 && twoNumbers >= 1)
                    totalScore = 0;
            }
            return totalScore;
        }

        private string GetLichessName(SwissSheetEntry entry)
            => entry.Username;

        private string GetLichessName(SheetEntry entry)
            => entry.Username;

        private DivisionType GetTournamentDivision(int rating)
        {
            if (DivisionC.InDivision(rating))
                return DivisionType.C;
            else if (DivisionB.InDivision(rating))
                return DivisionType.B;
            else if (DivisionA.InDivision(rating))
                return DivisionType.A;
            return DivisionType.None;
        }

        private DivisionType GetTournamentDivision(SwissSheetEntry entry)
            => GetTournamentDivision(entry.Rating);

        private DivisionType GetTournamentDivision(SheetEntry entry)
            => GetTournamentDivision(entry.Rating);

        private static TournamentRating<TValue> GenerateTournamentRating<TValue>(List<TValue> tournament, Func<TValue, DivisionType> getDivision, Func<TValue, string> getLichessName, Func<TValue, bool, int> calculateScore)
        {
            Dictionary<string, User?> users = [];
            using var reader = Program.Data.SelectQuery($"SELECT * FROM Users");
            {
                while (reader.Read())
                    if (!string.IsNullOrEmpty(reader.Get("LichessName")))
                        users.Add(reader.Get("LichessName"), new(reader.Get<long>("TelegramID"), reader.Get("Name"), reader.Get("LichessName"), reader.Get<int>("Year")));
            }

            Dictionary<DivisionType, List<TValue>> playersInDivision = new()
                {
                    { DivisionType.A, [] },
                    { DivisionType.B, [] },
                    { DivisionType.C, [] }
                };

            foreach (var entry in tournament)
            {
                var division = getDivision(entry);
                if (division != DivisionType.None && playersInDivision[division].Count < 3)
                    playersInDivision[division].Add(entry);
            }

            List<TournamentUser<TValue>> tournamentUsers = [];

            foreach (var entry in tournament)
            {
                bool inDivision = false;
                for (int i = 0; i < playersInDivision.Count; i++)
                    if (playersInDivision[(DivisionType)i].Contains(entry))
                    {
                        inDivision = true;
                        break;
                    }
                var score = calculateScore(entry, inDivision);
                if (users.TryGetValue(getLichessName(entry), out User? founded))
                    tournamentUsers.Add(new(founded, score, entry));
                else
                    tournamentUsers.Add(new(null, score, entry));
            }

            return new(playersInDivision, tournamentUsers);
        }

        private static string GetTournamentPath(string id)
            => Path.Combine(Environment.CurrentDirectory, "Tournaments", id + ".txt");

        private static string GetSwissTournamentPath(string id)
            => Path.Combine(Environment.CurrentDirectory, "SwissTournaments", id + ".txt");

        private struct TournamentRating<TValue>(Dictionary<DivisionType, List<TValue>> divisions, List<TournamentUser<TValue>> players)
        {
            public Dictionary<DivisionType, List<TValue>> Divisions = divisions;

            public List<TournamentUser<TValue>> Players = players;
        }

        private class TournamentUser<TValue>(User? user, int score, TValue entry)
        {
            public User? User = user;

            public int Score = score;

            public TValue TournamentEntry = entry;
        }

        private struct Division(int min, int max)
        {
            public int Min = min;

            public int Max = max;

            public bool InDivision(int rating)
                => rating >= Min && rating <= Max;
        }

        private enum DivisionType
        {
            A,
            B,
            C,
            None
        }
    }
}