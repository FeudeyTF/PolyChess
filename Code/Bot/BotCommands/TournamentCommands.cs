using LichessAPI.Types.Arena;
using PolyChessTGBot.Bot.Buttons;
using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Commands.Basic;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Bot.Messages.Discrete;
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

        [Button("SaveTournament")]
        internal async Task SaveTournament(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await DiscreteMessage.Send(
                    args.Query.Message.Chat.Id,
                    ["Введите ссылку на турнир"],
                    OnTournamentSwissEntered);

            static async Task OnTournamentSwissEntered(DiscreteMessageEnteredArgs args)
            {
                if (args.Answers.Length == 1)
                {
                    var tournamentLink = args.Answers[0].Text;
                    if (tournamentLink != null)
                    {
                        var splittedLink = tournamentLink.Split('/');
                        if (splittedLink.Length > 1)
                        {
                            var type = splittedLink[^2];
                            var id = splittedLink[^1];
                            if (!string.IsNullOrEmpty(type.Trim()) && !string.IsNullOrEmpty(id.Trim()))
                            {
                                if (type == "tournament")
                                {
                                    var tournament = await Program.Tournaments.UpdateTournament(id);
                                    if (tournament != null)
                                        await args.Reply($"Турнир <b>{tournament.Tournament.FullName}</b> был сохранён!");
                                     else
                                        await args.Reply("Турнир не был найден!");
                                }
                                else if (type == "swiss")
                                {
                                    var tournament = await Program.Tournaments.UpdateSwissTournament(id);
                                    if (tournament != null)
                                        await args.Reply($"Турнир <b>{tournament.Tournament.Name}</b> был сохранён!");
                                    else
                                        await args.Reply("Турнир не был найден!");
                                }
                                else
                                    await args.Reply("Неправильная ссылка!");
                            }
                            else
                                await args.Reply("Неправильная ссылка!");
                        }
                        else
                            await args.Reply("Неправильная ссылка!");
                    }
                    else
                        await args.Reply("Необходимо ввести ссылку на турнир!");
                }
            }
        }

        [Button("TournamentResult")]
        internal async Task TournamentResult(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await DiscreteMessage.Send(
                    args.Query.Message.Chat.Id,
                    [
                        "Введите ссылку на турнир",
                        "Введите тех, кого не нужно учитывать (разделять пробелами или запятой. Введите -, если все учитываются)"
                    ],
                    OnTournamentResultEntered);

            static async Task OnTournamentResultEntered(DiscreteMessageEnteredArgs args)
            {
                if (args.Answers.Length == 2)
                {
                    var tournamentLink = args.Answers[0].Text;
                    if (tournamentLink != null)
                    {
                        List<string> exclude = new(Program.MainConfig.TopPlayers);
                        var toExclude = args.Answers[1].Text;
                        if (toExclude != null && toExclude.Trim() != "-")
                        {
                            var stringsToExclude = toExclude.Split(' ').Select(p => p.Split(','));
                            foreach (var str in stringsToExclude)
                                foreach (var str2 in str)
                                    if (!string.IsNullOrEmpty(str2.Trim()))
                                        exclude.Add(str2.Trim());
                        }

                        var splittedLink = tournamentLink.Split('/');
                        if(splittedLink.Length > 1)
                        {
                            List<TelegramMessageBuilder> messages = [];
                            var type = splittedLink[^2];
                            var id = splittedLink[^1];
                            if (!string.IsNullOrEmpty(type.Trim()) && !string.IsNullOrEmpty(id.Trim()))
                            {
                                if (type == "tournament")
                                    messages = await OnArenaResultEntered(id, exclude);
                                else if (type == "swiss")
                                    messages = await OnSwissResultEntered(id, exclude);
                                else
                                {
                                    await args.Reply("Неправильная ссылка!");
                                    return;
                                }

                                foreach (var msg in messages)
                                    await args.Reply(msg);
                            }
                            else
                                await args.Reply("Неправильная ссылка!");
                        }
                        else
                            await args.Reply("Неправильная ссылка!");
                    }
                    else
                        await args.Reply("Необходимо ввести ссылку на турнир!");
                }
            }

            static async Task<List<TelegramMessageBuilder>> OnArenaResultEntered(string tournamentId, List<string> exclude)
            {
                List<TelegramMessageBuilder> result = [];
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
                        var stream = File.Open(csvFilePath, FileMode.Open);
                        message.WithFile(stream, "Table.csv");
                        result.Add(string.Join('\n', text));
                        result.Add(message);
                    }
                    else
                        result.Add("Турнир не был найден!");
                }
                else
                    result.Add("Турнир не сохранён с помощью команды /savearena!");
                return result;
            }

            static async Task<List<TelegramMessageBuilder>> OnSwissResultEntered(string tournamentId, List<string> exclude)
            {
                List<TelegramMessageBuilder> result = [];
                var tournament = await Program.Lichess.GetSwissTournament(tournamentId);

                var directory = Path.Combine(Environment.CurrentDirectory, "SwissTournaments");
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                var filePath = Path.Combine(directory, tournamentId + ".txt");
                if (File.Exists(filePath))
                {
                    var tournamentSheet = await Program.Lichess.GetSwissTournamentSheet(File.OpenText(filePath));
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
                        var stream = File.Open(csvFilePath, FileMode.Open);
                        message.WithFile(stream, "Table.csv");
                        result.Add(string.Join('\n', text));
                        result.Add(message);
                    }
                    else
                        result.Add("Турнир не был найден!");
                }
                else
                    result.Add("Турнир не сохранён с помощью команды /saveswiss!");

                return result;
            }
        }
    }
}