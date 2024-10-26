using LichessAPI.Types.Arena;
using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Extensions;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChessTGBot.Bot.BotCommands
{
    public partial class BotCommands
    {
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
                var tournament = await Program.Tournaments.UpdateTournament(tournamentId);
                if (tournament != null)
                    await args.Reply($"Турнир <b>{tournament.Tournament.FullName}</b> был сохранён!");
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
                var tournament = await Program.Tournaments.UpdateSwissTournament(tournamentId);
                if (tournament != null)
                    await args.Reply($"Турнир <b>{tournament.Tournament.Name}</b> был сохранён!");
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

                        var tournamentRating = Program.Tournaments.GenerateTournamentRating(tournamentSheet);

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
                    List<SheetEntry> tournamentSheet = [];
                    using (var file = File.OpenText(filePath))
                    {
                        tournamentSheet = await Program.Lichess.GetTournamentSheet(file);
                    }

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
                        tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username) || e.Team != null && !Program.MainConfig.InstitutesTeamsIDs.Contains(e.Team))).ToList();
                        List<string> csv = ["Имя;Ник Lichess;Балл"];
                        List<string> text = [
                            $"Турнир <b>{tournament.FullName}</b>. Состоялся <b>{tournament.StartDate:g}</b>",
                            $"Информация об участии в турнире"
                        ];

                        var tournamentRating = Program.Tournaments.GenerateTournamentRating(tournamentSheet);

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
    }
}