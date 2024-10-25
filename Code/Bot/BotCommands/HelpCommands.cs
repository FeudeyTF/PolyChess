using PolyChessTGBot.Bot.Buttons;
using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Database;
using PolyChessTGBot.Extensions;
using LichessAPI.Types.Arena;
using LichessAPI.Types.Swiss;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChessTGBot.Bot.BotCommands
{
    public partial class BotCommands
    {
        private readonly ListMessage<FAQEntry> FAQMessage;

        private readonly ListMessage<HelpLink> HelpMessage;

        private readonly List<FAQEntry> FAQEntries;

        private readonly List<HelpLink> HelpLinks;

        private readonly ListMessage<object> Tournaments;

        private Dictionary<long, (string Name, string FlairID)> AccountVerifyCodes;

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

            Tournaments = new("tournaments", GetTournamentsIDs, TournamentToString, 5, true, "Далее ➡️", "⬅️ Назад")
            {
                Header = "<b> - Информация об участии в турнирах!</b>"
            };

            AccountVerifyCodes = [];
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
        private async Task Question(CommandArgs args)
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

        [Command("cmds", "Выдаёт список всех команд", admin: true)]
        private async Task GetCommands(CommandArgs args)
        {
            List<string> text = ["<b>Список команд для бота:</b>", "", " - Обычные команды:"];
            foreach (var command in Program.Bot.CommandRegistrator.Commands)
                if (!command.AdminCommand)
                    text.Add($"<b>{command.Name}</b> - {command.Description}");
            text.Add("");
            text.Add(" - Админские команды");
            foreach (var command in Program.Bot.CommandRegistrator.Commands)
                if (command.AdminCommand)
                    text.Add($"<b>{command.Name}</b> - {command.Description}");

            await args.Reply(string.Join("\n", text));
        }

        [Command("faq", "Выдаёт список с FAQ", true)]
        private async Task FAQ(CommandArgs args)
        {
            await FAQMessage.Send(args.Bot, args.Message.Chat.Id, args.User);
        }

        [Command("reg", "Меняет привязанный к ученику аккаунт Lichess")]
        private async Task Register(CommandArgs args)
        {
            if (AccountVerifyCodes.TryGetValue(args.User.Id, out (string Name, string FlairID) code))
            {
                var account = await Program.Lichess.GetUserAsync(code.Name);
                if (account != null )
                {
                    if (account.Flair == code.FlairID)
                    {
                        using var reader = Program.Data.SelectQuery($"SELECT * FROM Users WHERE LichessName='{account.Username}'");
                        if (!reader.Read())
                        {
                            Program.Data.Query($"UPDATE Users SET LichessName='{account.Username}' WHERE TelegramID='{args.User.Id}'");
                            AccountVerifyCodes.Remove(args.User.Id);
                            await args.Reply($"Ваш аккаунт теперь - <b>{account.Username}</b>");
                        }
                        else
                            await args.Reply($"Аккаунт <b>{account.Username}</b> уже занят!");
                    }
                    else
                        await args.Reply($"Значок аккаунта <b>{code.Name}</b> не установлен на <b>{code.FlairID}</b> или отсутствует");
                }
                else
                    await args.Reply($"Аккаунт <b>{code.Name}</b> не был найден");
            }
            else
            {
                if (args.Parameters.Count == 1)
                {
                    var account = await Program.Lichess.GetUserAsync(args.Parameters[0]);
                    if (account != null)
                    {
                        using var reader = Program.Data.SelectQuery($"SELECT * FROM Users WHERE LichessName='{account.Username}'");
                        if (!reader.Read())
                        {
                            var flairCode = Program.MainConfig.Flairs[Random.Shared.Next(Program.MainConfig.Flairs.Count)];
                            while (flairCode == account.Flair)
                                flairCode = Program.MainConfig.Flairs[Random.Shared.Next(Program.MainConfig.Flairs.Count)];
                            AccountVerifyCodes.Add(args.User.Id, (account.Username, flairCode));
                            await args.Reply($"Вам нужно установить значок аккаунта {account.Username} на <b>{flairCode}</b> (делается в настройках на Lichess. Нужно вставить в поле выбора значка <b>{flairCode.Split('.')[1]}</b>), после чего найти точное совпадение значка с <b>{flairCode}</b>. Дальше вы опять прописываете /reg");

                        }
                        else
                            await args.Reply($"Аккаунт <b>{account.Username}</b> уже занят!");
                    }
                    else
                        await args.Reply($"Аккаунт <b>{args.Parameters[0]}</b> не был найден!");
                }
                else
                    await args.Reply("Неправильный синтаксис! Правильно: /reg ник Lichess");
            }
        }

        [Command("me", "Выдаёт информацию об ученике", true)]
        private async Task MyInfo(CommandArgs args)
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
                        text.Add($"♟ <b>Имя аккаунта на Lichess:</b> {lichessUser.Username}");
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
                        await args.Reply($"Ник на Lichess: <b>Аккаунт не найден</b>. Перепривяжите аккаунт с помощью /reg");
                }
                else
                    await args.Reply($"Ник на Lichess: <b>Аккаунт не привязан</b>. Привяжите аккаунт с помощью /reg");
            }
            else
                await args.Reply("Информация о Вашем аккаунте не найдена, обратитесь к администратору бота!");
        }

        [Button("MeViewTournaments")]
        private async Task ViewTournaments(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await Tournaments.Send(args.Bot, args.Query.Message.Chat.Id, args.Query.From);
        }

        private async Task<List<object>> GetTournamentsIDs()
        {
            List<object> result = [];
            foreach (var tournament in TournamentsList)
                if (tournament.Tournament.StartDate < DateTime.UtcNow)
                    result.Add(tournament);
            foreach (var tournament in SwissTournamentsList)
                if (tournament.Tournament.Started < DateTime.UtcNow)
                    result.Add(tournament);
            return await Task.FromResult(new List<object>([.. from r in result orderby (r is ArenaTournamentInfo t ? t.Tournament.StartDate : r is SwissTournamentInfo s ? s.Tournament.Started : DateTime.Now) descending select r]));
        }

        private async Task<string> TournamentToString(object info, int index, Telegram.Bot.Types.User tgUser)
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
                        if (info is ArenaTournamentInfo arenaTournamentInfo)
                        {
                            if (arenaTournamentInfo != null)
                            {
                                result.Add($"\U0001f91d Турнир <b><a href=\"https://lichess.org/tournament/{arenaTournamentInfo.Tournament.ID}\">{arenaTournamentInfo.Tournament.FullName}</a></b>. Состоялся <b>{arenaTournamentInfo.Tournament.StartDate.AddHours(3):g}</b>");
                                TournamentUser<SheetEntry>? player = default;
                                foreach (var p in arenaTournamentInfo.Rating.Players)
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
                                    foreach (var div in arenaTournamentInfo.Rating.Divisions)
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
                        else if (info is SwissTournamentInfo swissTournamentInfo)
                        {
                            if (swissTournamentInfo != null)
                            {
                                result.Add($"🇨🇭 Турнир <b><a href=\"https://lichess.org/swiss/{swissTournamentInfo.Tournament.ID}\">{swissTournamentInfo.Tournament.Name}</a></b>. Состоялся <b>{swissTournamentInfo.Tournament.Started.AddHours(3):g}</b>");
                                TournamentUser<SwissSheetEntry>? player = default;
                                foreach (var p in swissTournamentInfo.Rating.Players)
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
                                    foreach (var div in swissTournamentInfo.Rating.Divisions)
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
                        result.Add("");
                        return string.Join("\n", result);
                    }
                }
            }
            return string.Empty;
        }
    }
}
