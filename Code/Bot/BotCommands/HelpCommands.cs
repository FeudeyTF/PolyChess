using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Database;
using PolyChessTGBot.Externsions;
using PolyChessTGBot.Lichess.Types.Arena;
using PolyChessTGBot.Lichess.Types.Swiss;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace PolyChessTGBot.Bot.BotCommands
{
    public partial class BotCommands
    {
        private readonly ListMessage<FAQEntry> FAQMessage;

        private readonly ListMessage<HelpLink> HelpMessage;

        private readonly ListMessage<HelpLink> HelpAdmin;

        private readonly ListMessage<FAQEntry> FAQAdmin;

        private readonly List<FAQEntry> FAQEntries;

        private readonly List<HelpLink> HelpLinks;

        public BotCommands()
        {
            FAQMessage = new("FAQ", GetFAQValues, ConvertFAQEntryToString)
            {
                Header = "❓<b>FAQ</b> шахмат❓ Все самые <b>часто задаваемые</b> вопросы собраны в одном месте:"
            };

            HelpMessage = new("Help", GetHelpLinksValue, ConvertHelpLinkToString, 1, false, "Далее ➡️", "⬅️ Назад")
            {
                GetDocumentID = GetHelpLinkDocumentID
            };

            FAQAdmin = new("adminFAQ", GetFAQValues, ConvertFAQEntryToString, 1, additionalKeyboards: [[new("🗑Удалить", "Delete", HandleFAQDelete)]]);

            HelpAdmin = new("adminHelp", GetHelpLinksValue, ConvertHelpLinkToString, 1, false, "Далее ➡️", "⬅️ Назад", [[new("🗑Удалить", "Delete", HandleHelpLinkDelete)]])
            {
                GetDocumentID = GetHelpLinkDocumentID
            };

            FAQEntries = Program.Data.GetFAQEntries();
            HelpLinks = Program.Data.GetHelpLinks();
        }

        private List<HelpLink> GetHelpLinksValue() => HelpLinks;

        private List<FAQEntry> GetFAQValues() => FAQEntries;

        private string ConvertHelpLinkToString(HelpLink link, int index)
            => $"<b>{link.Title}</b>\n{link.Text}\n<i>{link.Footer}</i>";

        private string? GetHelpLinkDocumentID(HelpLink link) => link.FileID;

        private string ConvertFAQEntryToString(FAQEntry entry, int index)
            => $"{index + 1}) <b>{entry.Question}</b>\n - {entry.Answer}";

        [Command("question", "Синтаксис: /question \"вопрос\". Команда отправит вопрос напрямую Павлу", true)]
        public async Task Question(CommandArgs args)
        {
            string question = string.Join(" ", args.Parameters);
            if (!string.IsNullOrEmpty(question))
            {
                List<string> text =
                [
                    "<b><u>Вопрос от пользователя!</u></b>🙋‍",
                    $"👤<b>Ник пользователя:</b> @{args.User.Username}",
                    $"👤<b>Имя пользователя:</b> {args.User.FirstName} {args.User.LastName}",
                    $"🕑<b>Дата отправки:</b> {args.Message.Date:G}",
                    $"❓<b>Вопрос:</b>\n{question}"
                ];
                InlineKeyboardButton button = new("Данные");
                button.SetData("QuestionDataID", ("ID", args.User.Id), ("ChannelID", args.Message.MessageId));
                var message = new TelegramMessageBuilder(string.Join("\n", text))
                    .AddButton(button);
                await args.Bot.SendMessage(message, Program.MainConfig.QuestionChannel);
                await args.Reply("Ваш вопрос был успешно отправлен!");
            }
            else
                await args.Reply("Неправильно введён вопрос!");
        }

        [Command("help", "Выдаёт список с полезными материалами", true)]
        private async Task SendHelpLinks(CommandArgs args)
        {
            await HelpMessage.Send(args.Bot, args.Message.Chat.Id);
        }

        [Command("faq", "Выдаёт список с FAQ", true)]
        public async Task FAQ(CommandArgs args)
        {
            await FAQMessage.Send(args.Bot, args.Message.Chat.Id);
        }

        [Command("myinfo", "Выдаёт информацию об ученике", true)]
        public async Task MyInfo(CommandArgs args)
        {
            using var reader = Program.Data.SelectQuery($"SELECT * FROM Users WHERE TelegramID={args.User.Id}");
            if (reader.Read())
            {
                User user = new(reader.Get<long>("TelegramID"), reader.Get("Name"), reader.Get("LichessName"), reader.Get<int>("Year"));
                List<string> text = [$"👋 Приветствую, <b>{user.Name}</b>"];
                if (!string.IsNullOrEmpty(user.LichessName))
                {
                    TelegramMessageBuilder message = new();
                    var lichessUser = await Program.Lichess.GetUserAsync(user.LichessName);
                    if (lichessUser != null)
                    {
                        text.Add($"♟ <b> Имя аккаунта на Lichess:</b> {lichessUser.Username}");
                        text.Add($"🕓 <b>Дата регистрации:</b> {lichessUser.RegisterDate:g}");
                        text.Add($"🕓 <b>Последний вход:</b> {lichessUser.LastSeenDate:g}");
                        text.Add("👥 <i><b>Команды</b></i>");

                        var teams = await Program.Lichess.GetUserTeamsAsync(lichessUser.Username);

                        if (teams.Count > 0)
                            foreach (var team in teams)
                                text.Add($" - <b>{team.Name} ({team.MembersCount} участников)</b>");
                        else
                            text.Add(" - Отсутствуют");

                        text.Add("");
                        text.Add("📈 <i><b>Рейтинги</b></i>");
                        text.Add("");

                        foreach (var perfomance in lichessUser.Perfomance)
                            text.Add($" - <b>{perfomance.Key.Beautify()}</b>, Сыграно: {perfomance.Value.Games}, Рейтинг: {perfomance.Value.Rating}");

                        text.Add("");
                        text.Add("🤝 <i><b>Обычные турниры</b></i>");
                        text.Add("");

                        foreach (var file in Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Tournaments")))
                        {
                            var tournamentID = Path.GetFileName(file)[..^4];
                            var tournament = await Program.Lichess.GetTournament(tournamentID);
                            if (tournament != null)
                            {
                                var tournamentSheet = await Program.Lichess.GetTournamentSheet(File.OpenText(file));
                                if (tournamentSheet != null)
                                {
                                    List<string> exclude = new(Program.MainConfig.TopPlayers);
                                    tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username) || e.Team != null && !Program.MainConfig.PolytechTeams.Contains(e.Team))).ToList();
                                    var tournamentRating = GenerateTournamentRating(tournamentSheet, GetTournamentDivision, GetLichessName, CalculateScore);

                                    text.Add($"Турнир <b><a href=\"https://lichess.org/tournament/{tournament.ID}\">{tournament.FullName}</a></b>. Состоялся <b>{tournament.Started.AddHours(3):g}</b>");
                                    TournamentUser<SheetEntry>? player = default;
                                    foreach (var p in tournamentRating.Players)
                                    {
                                        if (p.TournamentEntry.Username == lichessUser.Username)
                                        {
                                            player = p;
                                            break;
                                        }
                                    }

                                    if (player != default)
                                    {
                                        text.Add($" - Ранг: <b>{player.TournamentEntry.Rank}</b>");
                                        text.Add($" - Рейтинг: <b>{player.TournamentEntry.Rating}</b>");
                                        text.Add($" - Перформанс: <b>{player.TournamentEntry.Performance}</b>");
                                        text.Add($" - Балл: <b>{(player.Score == -1 ? "Отсутствовал" : player.Score)}</b>");
                                        if (player.TournamentEntry.Sheet != null && !string.IsNullOrEmpty(player.TournamentEntry.Sheet.Scores))
                                            text.Add($" - Итоговая строка: <b>{player.TournamentEntry.Sheet.Scores}</b>");
                                        DivisionType division = DivisionType.None;
                                        foreach (var div in tournamentRating.Divisions)
                                            if (div.Value.Any(e => e.Username == lichessUser.Username))
                                            {
                                                division = div.Key;
                                                break;
                                            }

                                        text.Add($" - Двизион: <b>{(division == DivisionType.None ? "Нет" : division)}</b>");
                                       
                                    }
                                    else
                                        text.Add(" - <b>Отсутствовал</b>");
                                    text.Add("");
                                }
                            }
                        }

                        text.Add("🇨🇭 <i><b>Швейцарские турниры</b></i>");
                        text.Add("");

                        foreach (var file in Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "SwissTournaments")))
                        {
                            var tournamentID = Path.GetFileName(file)[..^4];
                            var tournament = await Program.Lichess.GetSwissTournament(tournamentID);
                            if (tournament != null)
                            {
                                var tournamentSheet = await Program.Lichess.GetSwissTournamentSheet(File.OpenText(file));
                                if (tournamentSheet != null)
                                {
                                    List<string> exclude = new(Program.MainConfig.TopPlayers);
                                    tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username))).ToList();
                                    var tournamentRating = GenerateTournamentRating(tournamentSheet, GetTournamentDivision, GetLichessName, CalculateScore);

                                    text.Add($"Турнир <b><a href=\"https://lichess.org/swiss/{tournament.ID}\">{tournament.Name}</a></b>. Состоялся <b>{tournament.Started.AddHours(3):g}</b>");
                                    TournamentUser<SwissSheetEntry>? player = default;
                                    foreach (var p in tournamentRating.Players)
                                    {
                                        if (p.TournamentEntry.Username == lichessUser.Username)
                                        {
                                            player = p;
                                            break;
                                        }
                                    }

                                    if (player != default)
                                    {
                                        text.Add($" - Ранг: <b>{player.TournamentEntry.Rank}</b>");
                                        text.Add($" - Очки: <b>{player.TournamentEntry.Points}</b>");
                                        text.Add($" - TieBreak: <b>{player.TournamentEntry.TieBreak}</b>");
                                        text.Add($" - Перформанс: <b>{player.TournamentEntry.Performance}</b>");
                                        text.Add($" - Балл: <b>{(player.Score == -1 ? "Отсутствовал" : player.Score)}</b>");
                                        DivisionType division = DivisionType.None;
                                        foreach (var div in tournamentRating.Divisions)
                                            if (div.Value.Any(e => e.Username == lichessUser.Username))
                                            {
                                                division = div.Key;
                                                break;
                                            }

                                        text.Add($" - Двизион: <b>{(division == DivisionType.None ? "Нет" : division)}</b>");
                                    }
                                    else
                                        text.Add(" - <b>Отсутствовал</b>");
                                    text.Add("");
                                }
                            }
                        }
                        message.WithoutWebPagePreview();
                        message.WithText(string.Join("\n", text));
                        InlineKeyboardButton accountLinkButton =
                          new("♟Lichess профиль")
                          {
                              Url = lichessUser.URL
                          };
                        message.AddButton(accountLinkButton);

                        await args.Reply(message);
                    }
                    else
                        text.Add($"Ник на Lichess: <b>Аккаунт не найден</b>");
                }
                else
                    text.Add($"Ник на Lichess: <b>Аккаунт не привязан</b>");
            }
            else
                await args.Reply("Информация о Вашем аккаунте не найдена, обратитесь к администратору бота!");
        }
    }
}
