using PolyChess.Components.Data;
using PolyChess.Components.Data.Tables;
using PolyChess.Components.Telegram.Commands;
using PolyChess.Components.Tournaments;
using PolyChess.Configuration;
using PolyChess.Core.Telegram;
using PolyChess.Core.Telegram.Messages;
using PolyChess.Core.Telegram.Messages.Discrete;
using PolyChess.Core.Telegram.Messages.Discrete.Messages;

namespace PolyChess.Components.Telegram.ClientCommands
{
    internal class AdminCommands : TelegramCommandAggregator
    {
        private readonly PolyContext _polyContext;

        private readonly TournamentsComponent _tournaments;

        private readonly IMainConfig _mainConfig;

        private readonly DiscreteMessagesProvider _discreteMessagesProvider;

        public AdminCommands(PolyContext polyContext, ITelegramProvider telegramProvider, TournamentsComponent tournaments, IMainConfig config)
        {
            _polyContext = polyContext;
            _tournaments = tournaments;
            _mainConfig = config;
            _discreteMessagesProvider = new(telegramProvider);
        }

        [TelegramCommand("addlesson", "Добавляет урок", IsAdmin = true, IsHidden = true)]
        private async Task AddLesson(TelegramCommandExecutionContext ctx, DateTime startDate, DateTime endDate, float? latitude = default, float? longitude = default)
        {
            if (startDate >= endDate)
            {
                await ctx.ReplyAsync("Ошибка! Дата начала идёт после даты конца.");
                return;
            }

            latitude ??= _mainConfig.SchoolLocation.X;
            longitude ??= _mainConfig.SchoolLocation.Y;

            Lesson lesson = new()
            {
                StartDate = startDate,
                EndDate = endDate,
                Latitude = latitude.Value,
                Longitude = longitude.Value
            };

            _polyContext.Lessons.Add(lesson);
            await _polyContext.SaveChangesAsync();
            await ctx.SendMessageAsync(new TelegramMessageBuilder($"Урок с <b>{startDate:g} до {endDate:g}</b> успешно добавлен!"), ctx.Message.Chat.Id);
        }

        [TelegramCommand("updatetournaments", "Выгружает турниры с Lichess в локальное хранилище", IsAdmin = true, IsHidden = true)]
        private async Task UpdateTournaments(TelegramCommandExecutionContext ctx)
        {
            if (_mainConfig.TeamsWithTournaments.Count > 0)
            {
                foreach (var teamId in _mainConfig.TeamsWithTournaments)
                {
                    await ctx.ReplyAsync($"Началась загрузка турниров из {teamId}... Это может занять некоторое время");
                    var updatedTournaments = await _tournaments.UpdateTournaments(teamId);
                    if (updatedTournaments.Count > 0)
                        await ctx.ReplyAsync($"Турниры {string.Join(", ", updatedTournaments.Select(t => "<b>" + t.name + "</b>"))} успешно добавлены!");
                    else
                        await ctx.ReplyAsync("Все турниры уже загружены! Обновления не требуется");
                }
            }
            else
                await ctx.ReplyAsync("Команда Политеха не найдена!");
        }

        [TelegramCommand("addstudents", "Добавляет студентов из списка", IsAdmin = true, IsHidden = true)]
        private async Task AddStudents(TelegramCommandExecutionContext ctx)
        {
            DiscreteMessage message = new(
                _discreteMessagesProvider,
                [
                    new TelegramMessageBuilder("Введите данные студентов в формате Имя,Фамилия,Отчество,Курс,Институт,Lichess,LichessToken,TelegramId")
                ],
                HandleStudentsEntered
            );
            await ctx.SendMessageAsync(message, ctx.Message.Chat.Id);

            async Task HandleStudentsEntered(DiscreteMessageEnteredArgs args)
            {
                var response = args.Responses.First().Text;
                if (response == null)
                {
                    await args.ReplyAsync("Вы не ввели текст!");
                    return;
                }
                List<(string student, string error)> skippedStudents = [];
                var students = response.Split('\n');
                foreach (var student in students)
                {
                    var studentData = student.Split(',');
                    if (studentData.Length != 8)
                    {
                        skippedStudents.Add((student, "Не введены все данные (для пустых оставьте пропуск)"));
                        continue;
                    }

                    var name = studentData[0];
                    var surname = studentData[1];
                    var patronomic = studentData[2];
                    var yearStr = studentData[3];
                    var institute = studentData[4];
                    var lichess = studentData[5];
                    var lichessToken = studentData[6];
                    var telegramIdStr = studentData[7];

                    if (!int.TryParse(yearStr, out var year))
                    {
                        skippedStudents.Add((name, "Курс не является числом!"));
                        continue;
                    }

                    if (!long.TryParse(telegramIdStr, out var telegramId))
                    {
                        skippedStudents.Add((name, "TelegramId не является числом!"));
                        continue;
                    }

                    bool isSkipped = false;
                    foreach (var s in _polyContext.Students)
                    {
                        if (s.Name == name && s.Surname == surname && s.Patronymic == patronomic)
                        {
                            skippedStudents.Add((name, "Студент с таким именем уже существует!"));
                            isSkipped = true;
                        }
                        else if (s.LichessId == lichess)
                        {
                            skippedStudents.Add((name, "Студент с таким аккаунтом Lichess уже существует!"));
                            isSkipped = true;
                        }
                        else if (s.TelegramId == telegramId)
                        {
                            skippedStudents.Add((name, "Студент с таким аккаунтом Telegram уже существует!"));
                            isSkipped = true;
                        }
                    }

                    if (isSkipped)
                        continue;

                    Student studentEntry = new()
                    {
                        Name = name,
                        Surname = surname,
                        Patronymic = patronomic,
                        Year = year,
                        TelegramId = telegramId,
                        LichessId = lichess,
                        LichessToken = lichessToken,
                        Institute = institute
                    };
                    _polyContext.Students.Add(studentEntry);
                }
                await _polyContext.SaveChangesAsync();
                await args.ReplyAsync($"Успешно добавлено {students.Length - skippedStudents.Count} студентов! Пропущенные студенты:\n{string.Join('\n', skippedStudents.Select(s => s.student + ":" + s.error))}");
            }
        }
    }
}
