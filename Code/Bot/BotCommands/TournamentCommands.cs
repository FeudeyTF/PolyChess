using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Commands.Basic;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Extensions;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChessTGBot.Bot.BotCommands
{
    public partial class BotCommands
    {
        [Command("getplayerscore", "Показывает результат участия студента в турнире", admin: true)]
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
                            await args.Reply($"Студент {name} не участовал в турнире");
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
                await args.Reply("Неправильный синтаксис! Правильно: /getplayerscore \"ID турнира\" \"Ник студента\"");
        }
    }
}