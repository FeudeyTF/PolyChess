using PolyChessTGBot.Bot.Buttons;
using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Database;
using PolyChessTGBot.Externsions;
using PolyChessTGBot.Lichess.Types.Arena;
using PolyChessTGBot.Lichess.Types.Swiss;
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

        private readonly ListMessage<TournamentInfo> Tournaments;

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

            Tournaments = new("tournaments", GetTournamentsIDs, TournamentToString, 5, false, "Далее ➡️", "⬅️ Назад")
            {
                Header = "<b> - Информация об участии в турнирах!</b>"
            };

            FAQEntries = Program.Data.GetFAQEntries();
            HelpLinks = Program.Data.GetHelpLinks();
        }

        private async Task<List<HelpLink>> GetHelpLinksValue() => await Task.FromResult(HelpLinks);

        private async Task<List<FAQEntry>> GetFAQValues() => await Task.FromResult(FAQEntries);

        private async Task<string> ConvertHelpLinkToString(HelpLink link, int index, Telegram.Bot.Types.User user)
            => await Task.FromResult($"<b>{link.Title}</b>\n{link.Text}\n<i>{link.Footer}</i>");

        private string? GetHelpLinkDocumentID(HelpLink link) => link.FileID;

        private async Task<string> ConvertFAQEntryToString(FAQEntry entry, int index, Telegram.Bot.Types.User user)
            => await Task.FromResult($"{index + 1}) <b>{entry.Question}</b>\n - {entry.Answer}");

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
            await HelpMessage.Send(args.Bot, args.Message.Chat.Id, args.User);
        }

        [Command("faq", "Выдаёт список с FAQ", true)]
        public async Task FAQ(CommandArgs args)
        {
            await FAQMessage.Send(args.Bot, args.Message.Chat.Id, args.User);
        }

        [Command("me", "Выдаёт информацию об ученике", true)]
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

                        foreach (var perfomance in lichessUser.Perfomance)
                            text.Add($" - <b>{perfomance.Key.Beautify()}</b>, Двизион: <b>{GetTournamentDivision(perfomance.Value.Rating)}</b>, Сыграно: {perfomance.Value.Games}, Рейтинг: {perfomance.Value.Rating}");

                        message.WithoutWebPagePreview();
                        message.WithText(string.Join("\n", text));
                        InlineKeyboardButton accountLinkButton =
                          new("♟Lichess профиль")
                          {
                              Url = lichessUser.URL
                          };
                        message.AddButton(accountLinkButton);

                        InlineKeyboardButton viewTournaments = new("💪 Посмотреть баллы за турниры");
                        viewTournaments.SetData("MeViewTournaments");
                        message.AddButton(viewTournaments);
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

        [Button("MeViewTournaments")]
        private async Task ViewTournaments(ButtonInteractArgs args)
        {
            if(args.Query.Message != null)
                await Tournaments.Send(args.Bot, args.Query.Message.Chat.Id, args.Query.From);
        }

        private async Task<List<TournamentInfo>> GetTournamentsIDs()
        {
            List<(TournamentInfo Info, DateTime Date)> result = [];
            foreach (var filePath in Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Tournaments")))
            {
                var tournament = await Program.Lichess.GetTournament(Path.GetFileName(filePath)[..^4]);
                if(tournament != null)
                    result.Add(new(new(tournament.ID, TournamentType.Default), tournament.Started));
            }

            foreach (var filePath in Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "SwissTournaments")))
            {
                var tournament = await Program.Lichess.GetSwissTournament(Path.GetFileName(filePath)[..^4]);
                if (tournament != null)
                    result.Add(new(new(tournament.ID, TournamentType.Swiss), tournament.Started));
            }

            result = [..from r in result orderby r.Date descending select r];
            return [..result.Select(r => r.Info)];
        }

        private async Task<string> TournamentToString(TournamentInfo info, int index, Telegram.Bot.Types.User tgUser)
        {
            using var reader = Program.Data.SelectQuery($"SELECT * FROM Users WHERE TelegramID={tgUser.Id}");
            if (reader.Read())
            {
                User user = new(reader.Get<long>("TelegramID"), reader.Get("Name"), reader.Get("LichessName"), reader.Get<int>("Year"));
                if (!string.IsNullOrEmpty(user.LichessName))
                {
                    TelegramMessageBuilder message = new();
                    var lichessUser = await Program.Lichess.GetUserAsync(user.LichessName);
                    if (lichessUser != null)
                    {
                        List<string> result = [];
                        if (info.Type == TournamentType.Default || info.Type == TournamentType.Team)
                        {
                            var tournament = await Program.Lichess.GetTournament(info.ID);
                            if (tournament != null)
                            {
                                if (!File.Exists(GetTournamentPath(info.ID)))
                                    return "";
                                var tournamentSheet = await Program.Lichess.GetTournamentSheet(File.OpenText(GetTournamentPath(info.ID)));
                                if (tournamentSheet != null)
                                {
                                    List<string> exclude = new(Program.MainConfig.TopPlayers);
                                    tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username) || e.Team != null && !Program.MainConfig.PolytechTeams.Contains(e.Team))).ToList();
                                    var tournamentRating = GenerateTournamentRating(tournamentSheet, GetTournamentDivision, GetLichessName, CalculateScore);

                                    result.Add($"\U0001f91d Турнир <b><a href=\"https://lichess.org/tournament/{tournament.ID}\">{tournament.FullName}</a></b>. Состоялся <b>{tournament.Started.AddHours(3):g}</b>");
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
                                        result.Add($" - Ранг: <b>{player.TournamentEntry.Rank}</b>");
                                        result.Add($" - Рейтинг: <b>{player.TournamentEntry.Rating}</b>");
                                        result.Add($" - Перформанс: <b>{player.TournamentEntry.Performance}</b>");
                                        result.Add($" - Балл: <b>{(player.Score == -1 ? "Турнир не зачтён" : player.Score)}</b>");
                                        if (player.TournamentEntry.Sheet != null && !string.IsNullOrEmpty(player.TournamentEntry.Sheet.Scores))
                                            result.Add($" - Итоговая строка: <b>{player.TournamentEntry.Sheet.Scores}</b>");
                                        DivisionType division = DivisionType.None;
                                        foreach (var div in tournamentRating.Divisions)
                                            if (div.Value.Any(e => e.Username == lichessUser.Username))
                                            {
                                                division = div.Key;
                                                break;
                                            }

                                        result.Add($" - Дивизион: <b>{(division == DivisionType.None ? "Нет" : division)}</b>");

                                    }
                                    else
                                        result.Add(" - <b>Отсутствовал</b>");
                                }
                            }
                        }
                        else
                        {
                            var tournament = await Program.Lichess.GetSwissTournament(info.ID);
                            if (tournament != null)
                            {
                                if (!File.Exists(GetSwissTournamentPath(info.ID)))
                                    return string.Empty;
                                var tournamentSheet = await Program.Lichess.GetSwissTournamentSheet(File.OpenText(GetSwissTournamentPath(info.ID)));
                                if (tournamentSheet != null)
                                {
                                    List<string> exclude = new(Program.MainConfig.TopPlayers);
                                    tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username))).ToList();
                                    var tournamentRating = GenerateTournamentRating(tournamentSheet, GetTournamentDivision, GetLichessName, CalculateScore);

                                    result.Add($"🇨🇭 Турнир <b><a href=\"https://lichess.org/swiss/{tournament.ID}\">{tournament.Name}</a></b>. Состоялся <b>{tournament.Started.AddHours(3):g}</b>");
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
                                        result.Add($" - Ранг: <b>{player.TournamentEntry.Rank}</b>");
                                        result.Add($" - Очки: <b>{player.TournamentEntry.Points}</b>");
                                        result.Add($" - TieBreak: <b>{player.TournamentEntry.TieBreak}</b>");
                                        result.Add($" - Перформанс: <b>{player.TournamentEntry.Performance}</b>");
                                        result.Add($" - Балл: <b>{(player.Score == -1 ? "Отсутствовал" : player.Score)}</b>");
                                        DivisionType division = DivisionType.None;
                                        foreach (var div in tournamentRating.Divisions)
                                            if (div.Value.Any(e => e.Username == lichessUser.Username))
                                            {
                                                division = div.Key;
                                                break;
                                            }

                                        result.Add($" - Дивизион: <b>{(division == DivisionType.None ? "Нет" : division)}</b>");
                                    }
                                    else
                                        result.Add(" - <b>Отсутствовал</b>");
                                }
                            }
                        }
                        result.Add("");
                        return string.Join("\n", result);
                    }
                }
            }
            return string.Empty;
        }

        private struct TournamentInfo(string id, TournamentType type)
        {
            public string ID = id;

            public TournamentType Type = type;
        }

        private enum TournamentType
        {
            Swiss,
            Default,
            Team
        }
    }
}
