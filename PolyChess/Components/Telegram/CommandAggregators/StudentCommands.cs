using Microsoft.EntityFrameworkCore;
using PolyChess.Components.Data;
using PolyChess.Components.Data.Tables;
using PolyChess.Components.Telegram.Buttons;
using PolyChess.Components.Telegram.Commands;
using PolyChess.Configuration;
using PolyChess.Core;
using PolyChess.Core.Telegram;
using PolyChess.Core.Telegram.Messages;
using PolyChess.Core.Telegram.Messages.Discrete;
using PolyChess.Core.Telegram.Messages.Discrete.Messages;
using PolyChess.Core.Telegram.Messages.Pagination;
using PolyChess.Core.Telegram.Messages.Pagination.Builders;
using PolyChess.LichessAPI.Clients;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChess.Components.Telegram.CommandAggregators
{
    internal class StudentCommands : TelegramCommandAggregator
    {
        private readonly DiscreteMessagesProvider _discreteMessagesProvider;

        private readonly PolyContext _polyContext;

        private readonly IMainConfig _mainConfig;

        private readonly LichessClient _lichess;

        private readonly Dictionary<long, (string Name, string FlairId)> _accountVerifyCodes;

        private readonly PaginationMessage<HelpEntry> _helpMessage;

        private readonly PaginationMessage<FaqEntry> _faqMessage;

        public StudentCommands(PolyContext polyContext, IMainConfig mainConfig, ITelegramProvider provider, LichessClient lichess)
        {
            _discreteMessagesProvider = new(provider);
            _polyContext = polyContext;
            _mainConfig = mainConfig;
            _accountVerifyCodes = [];
            _lichess = lichess;

            SimplePaginationMessageBuilder<HelpEntry> helpBuilder = new(HelpEntryToString);
            _helpMessage = new(nameof(_helpMessage)[1..], 1, GetHelpEntries, helpBuilder, provider);

            SimplePaginationMessageBuilder<FaqEntry> faqBuilder = new(FaqEntryToString);
            _faqMessage = new(nameof(_faqMessage)[1..], 5, GetFaqEntries, faqBuilder, provider);
        }


        [TelegramCommand("question", "Задаёт вопрос напрямую Павлу")]
        private async Task Question(TelegramCommandExecutionContext ctx)
        {
            if (!_polyContext.Students.Any(s => s.TelegramId == ctx.User.Id))
            {
                await ctx.ReplyAsync("Вы не студент!");
                return;
            }

            DiscreteMessage message = new(_discreteMessagesProvider, [new TelegramMessageBuilder("Введите ваш вопрос")], HandleQuestionEntered);
            await ctx.SendMessageAsync(message, ctx.Message.Chat.Id);

            async Task HandleQuestionEntered(DiscreteMessageEnteredArgs args)
            {
                var question = args.Responses[0];
                if (string.IsNullOrEmpty(question.Text))
                {
                    await args.ReplyAsync("Необходимо задать вопрос текстом");
                    return;
                }

                var student = _polyContext.Students.FirstOrDefault(s => s.TelegramId == args.User.Id);
                if (student != null)
                {
                    List<string> text =
                    [
                        "<b><u>Вопрос от пользователя!</u></b>🙋‍",
                        $"👤<b>Ник пользователя:</b> @{args.User.Username}",
                        $"👤<b>Имя студента:</b> {student.Name} {student.Surname} {student.Patronymic}",
                        $"🕑<b>Дата отправки:</b> {question.Date:G}",
                        $"❓<b>Вопрос:</b>\n{question.Text}"
                    ];

                    InlineKeyboardButton button = new("Данные");
                    button.SetData("QuestionDataID", ("ID", args.User.Id), ("ChannelID", question.MessageId));
                    var message = new TelegramMessageBuilder(string.Join("\n", text))
                        .AddButton(button);
                    await args.Client.SendMessageAsync(message, _mainConfig.QuestionChannelId, args.Token);
                    await args.ReplyAsync("Ваш вопрос был успешно отправлен!");
                }
                else
                    await args.ReplyAsync("Вы не студент!");
            }
        }

        [TelegramButton(nameof(CreativeTaskApprove))]
        private async Task CreativeTaskApprove(TelegramButtonExecutionContext ctx)
        {
            if (ctx.Query.Message != null)
            {
                var telegramId = ctx.GetLongNumber("ID");
                var student = _polyContext.Students.FirstOrDefault(s => s.TelegramId == telegramId);
                if (student != null)
                {
                    student.CreativeTaskCompleted = true;
                    await _polyContext.SaveChangesAsync();
                    await ctx.Provider.SendMessageAsync(new TelegramMessageBuilder($"Ваше творческое задание было <b>принято</b>! Поздравляю, вы - <b>молодец</b>"), telegramId);

                    TelegramMessageBuilder builder = new("[ПРИНЯТО]\n" + (ctx.Query.Message.Text ?? ctx.Query.Message.Caption));
                    if (ctx.Query.Message.Document != null)
                        builder.WithFile(ctx.Query.Message.Document.FileId);

                    await ctx.Provider.EditMessageAsync(ctx.Query.Message, builder);
                    await ctx.ReplyAsync($"Вы успешно <b>приняли</b> задание студента <b>{student.Name}</b>");
                }
                else
                    await ctx.ReplyAsync("Студент не был найден!");
            }
        }

        [TelegramCommand("task", "Отправляет выполненное творческое задание Павлу")]
        private async Task Task(TelegramCommandExecutionContext ctx)
        {
            DiscreteMessage message = new(
                _discreteMessagesProvider,
                [new TelegramMessageBuilder("Отправьте сообщение с файлом с выполненным заданием")],
                HandleTaskEntered
            );
            await ctx.Provider.SendMessageAsync(message, ctx.User.Id);

            async Task HandleTaskEntered(DiscreteMessageEnteredArgs args)
            {
                var msg = args.Responses[0];
                if (msg.Document != null)
                {
                    var student = _polyContext.Students.FirstOrDefault(s => s.TelegramId == ctx.User.Id);
                    if (student != null)
                    {
                        if (!student.CreativeTaskCompleted)
                        {
                            List<string> text = ["Пришло выполненное творческое задание!"];
                            text.Add($"Студент: <b>{student.Name}</b>");
                            text.Add($"Курс: <b>{student.Year}</b>");
                            text.Add($"Сообщение от студента:");
                            text.Add($"<b>{msg.Text ?? msg.Caption ?? "Студент не отправлял текстового сообщения"}</b>");
                            TelegramMessageBuilder message = new(string.Join("\n", text));
                            message.WithFile(msg.Document.FileId);

                            InlineKeyboardButton approveButton = new("✅ Принять");
                            approveButton.SetData(nameof(CreativeTaskApprove), ("ID", student.TelegramId));
                            InlineKeyboardButton declineButton = new("❌ Отклонить");
                            declineButton.SetData(nameof(CreativeTaskDecline), ("ID", student.TelegramId));

                            message.AddKeyboard([approveButton, declineButton]);

                            await ctx.Provider.SendMessageAsync(message, _mainConfig.CreativeTaskChannel);
                            await ctx.ReplyAsync("Творческое задание было успешно отправлено!");
                        }
                        else
                            await ctx.ReplyAsync("Вы уже выполнили творческое задание!");
                    }
                    else
                        await ctx.ReplyAsync("Вы не студент!");
                }
                else
                    await ctx.ReplyAsync("Вы не прикрепили файл к сообщению!");
            }
        }


        [TelegramButton(nameof(CreativeTaskDecline))]
        private async Task CreativeTaskDecline(TelegramButtonExecutionContext ctx)
        {
            DiscreteMessage discreteMessage = new(
                _discreteMessagesProvider,
                [new TelegramMessageBuilder("Введите причину отклонения")],
                OnCreativeTaskMessageEntered
            );

            if (ctx.Query.Message != null)
                await ctx.SendMessageAsync(discreteMessage, ctx.Query.Message.Chat.Id);

            async Task OnCreativeTaskMessageEntered(DiscreteMessageEnteredArgs args)
            {
                if (args.Responses.Length == 1 && ctx.Query.Message != null)
                {
                    var telegramId = ctx.GetLongNumber("ID");
                    var student = _polyContext.Students.FirstOrDefault(s => s.TelegramId == telegramId);
                    if (student != null)
                    {
                        TelegramMessageBuilder builder = new("[ОНТКЛОНЕНО]\n" + (ctx.Query.Message.Text ?? ctx.Query.Message.Caption));
                        if (ctx.Query.Message.Document != null)
                            builder.WithFile(ctx.Query.Message.Document.FileId);

                        await args.Client.EditMessageAsync(builder, ctx.Query.Message, args.Token);
                        await args.Client.SendMessageAsync($"Ваше творческое задание <b>было отклонено</b> по причине:\n{args.Responses[0].Text}", telegramId, args.Token);
                        await args.ReplyAsync($"Вы успешно <b>отклонили</b> задание студента <b>{student.Name}</b>");
                    }
                    else
                        await args.ReplyAsync("Студент не был найден!");
                }
            }
        }

        [TelegramCommand("register", "Регистрирует Вас в боте, как студента")]
        private async Task Register(TelegramCommandExecutionContext ctx)
        {
            if (_polyContext.Students.Any(s => s.TelegramId == ctx.User.Id))
            {
                await ctx.ReplyAsync("Вы уже зарегистрированы в боте!");
                return;
            }

            DiscreteMessage message = new(
                _discreteMessagesProvider,
                [new TelegramMessageBuilder("Введите своё ФИО")],
                HandleNameEntered
            );

            await ctx.SendMessageAsync(message, ctx.Message.Chat.Id);

            async Task HandleNameEntered(DiscreteMessageEnteredArgs args)
            {
                var message = args.Responses[0].Text;
                if (message == null)
                {
                    await args.ReplyAsync("Необходимо ввести ФИО");
                    return;
                }

                var splittedMessage = message.Split(' ');
                if (splittedMessage.Length != 3)
                {
                    await args.ReplyAsync("Необходимо сообщение только с ФИО, пример: Иванов Иван Иванович");
                    return;
                }

                var surname = splittedMessage[0];
                var name = splittedMessage[1];
                var patronomyc = splittedMessage[2];

                var student = _polyContext.Students.FirstOrDefault(s => s.Surname == surname && s.Patronymic == patronomyc && s.Name == name);
                if (student == null)
                {
                    await args.ReplyAsync("Ваше имя не найдено! Либо оно неправильно введено, либо его нет в базе!");
                    return;
                }

                if (student.TelegramId != default)
                {
                    await args.ReplyAsync("Этот аккаунт принадлежит другому!");
                    return;
                }

                student.TelegramId = ctx.User.Id;
                await _polyContext.SaveChangesAsync();
                await args.ReplyAsync("Регистрация успешно завершена! Привяжите свой Lichess аккаунт с помощью команды /lichess");
            }
        }

        [TelegramCommand("lichess", "Привязывает аккаунт Lichess к аккаунту")]
        private async Task Lichess(TelegramCommandExecutionContext ctx)
        {
            if (_accountVerifyCodes.TryGetValue(ctx.User.Id, out (string Name, string FlairId) code))
            {
                var account = await _lichess.GetUserAsync(code.Name);
                if (account != null)
                {
                    if (account.Flair == code.FlairId)
                    {
                        if (!_polyContext.Students.Any(s => s.LichessId == account.ID))
                        {
                            var student = _polyContext.Students.FirstOrDefault(s => s.TelegramId == ctx.User.Id);
                            if (student != null)
                            {
                                student.LichessId = account.ID;
                                await _polyContext.SaveChangesAsync();
                                _accountVerifyCodes.Remove(ctx.User.Id);
                                await ctx.ReplyAsync($"Ваш аккаунт теперь - <b>{account.Username}</b>");
                            }
                            else
                                await ctx.ReplyAsync($"Ваши данные не были найдены!");
                        }
                        else
                            await ctx.ReplyAsync($"Аккаунт <b>{account.Username}</b> уже занят!");
                    }
                    else
                        await ctx.ReplyAsync($"Значок аккаунта <b>{code.Name}</b> не установлен на <b>{code.FlairId}</b> или отсутствует");
                }
                else
                    await ctx.ReplyAsync($"Аккаунт <b>{code.Name}</b> не был найден");
            }
            else
            {
                DiscreteMessage message = new(
                    _discreteMessagesProvider,
                    [new TelegramMessageBuilder("Введите свой ник на Lichess")],
                    HandleLichessEntered
                );

                await ctx.SendMessageAsync(message, ctx.Message.Chat.Id);
            }

            async Task HandleLichessEntered(DiscreteMessageEnteredArgs args)
            {
                var name = args.Responses[0].Text;
                if (string.IsNullOrEmpty(name))
                {
                    await args.ReplyAsync("Вы не ввели ник!");
                    return;
                }

                var account = await _lichess.GetUserAsync(name);
                if (account != null)
                {
                    if (!_polyContext.Students.Any(s => s.LichessId == account.ID))
                    {
                        if (_mainConfig.LichessFlairs.Count == 0)
                        {
                            await args.ReplyAsync("Система привязки не настроена, обратитесь к администратору!");
                            return;
                        }

                        var flairCode = _mainConfig.LichessFlairs.Random();
                        while (flairCode == account.Flair)
                            flairCode = _mainConfig.LichessFlairs.Random();
                        _accountVerifyCodes.Add(args.User.Id, (account.Username, flairCode));
                        await args.ReplyAsync($"Вам нужно установить значок аккаунта {account.Username} на <b>{flairCode}</b> (делается в настройках на Lichess. Нужно вставить в поле выбора значка <b>{flairCode.Split('.')[1]}</b>), после чего найти точное совпадение значка с <b>{flairCode}</b>. Дальше вы опять прописываете /reg");
                    }
                    else
                        await args.ReplyAsync($"Аккаунт <b>{account.Username}</b> уже занят!");
                }
                else
                    await args.ReplyAsync($"Аккаунт <b>{name}</b> не был найден!");
            }
        }

        [TelegramCommand("help", "Выдаёт информацию с полезными ссылками")]
        private async Task Help(TelegramCommandExecutionContext ctx)
        {
            await ctx.SendMessageAsync(_helpMessage, ctx.Message.Chat.Id);
        }

        [TelegramCommand("faq", "Выдаёт информацию с ответами на частозадаваемые вопросы")]
        private async Task Faq(TelegramCommandExecutionContext ctx)
        {
            await ctx.SendMessageAsync(_faqMessage, ctx.Message.Chat.Id);
        }

        [TelegramCommand("attendance", "Показывает посещаемость")]
        private async Task Attendance(TelegramCommandExecutionContext ctx)
        {
            var student = _polyContext.Students.FirstOrDefault(s => s.TelegramId == ctx.User.Id);
            if (student == null)
            {
                await ctx.ReplyAsync("Вы не студент!");
                return;
            }

            var attendace = _polyContext.Attendances.Where(a => a.Student.TelegramId == student.TelegramId);
            List<string> msg =
            [
                "Ваша посещаемость:"
            ];

            if (!attendace.Any())
                msg.Add("Ни одного занятия не посещено!");

            foreach (var lesson in _polyContext.Lessons)
                if(lesson.StartDate < DateTime.Now)
                    msg.Add($"Занятие с {lesson.StartDate:g} до {lesson.EndDate:g}: {(attendace.Any(a => a.Lesson.Id == lesson.Id) ? "Посещено" : "Не посещено")}. Занятие {(lesson.IsRequired ? "обязательно" : "не обязательно")} для посещения");

            await ctx.ReplyAsync(string.Join("\n", msg));
        }

        private DbSet<FaqEntry> GetFaqEntries(Message message)
            => _polyContext.FaqEntries;

        private string FaqEntryToString(FaqEntry entry, int index)
            => $"{entry.Id}) <b>{entry.Question}</b>\n - {entry.Answer}";

        private DbSet<HelpEntry> GetHelpEntries(Message message)
            => _polyContext.HelpEntries;

        private string HelpEntryToString(HelpEntry entry, int index)
            => $"<b>{entry.Title}</b>\n{entry.Text}\n<i>{entry.Footer}</i>";
    }
}
