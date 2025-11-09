using PolyChess.Components.Data;
using PolyChess.Components.Data.Tables;
using PolyChess.Components.Telegram.Buttons;
using PolyChess.Components.Telegram.Commands;
using PolyChess.Components.Tournaments;
using PolyChess.Configuration;
using PolyChess.Core.Logging;
using PolyChess.Core.Telegram;
using PolyChess.Core.Telegram.Messages;
using PolyChess.Core.Telegram.Messages.Discrete;
using PolyChess.Core.Telegram.Messages.Discrete.Messages;
using PolyChess.LichessAPI.Clients;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChess.Components.Telegram.CommandAggregators
{
    internal class AdminPanel : TelegramCommandAggregator
    {
        private readonly PolyContext _polyContext;

        private readonly LichessClient _lichessClient;

        private readonly ILogger _logger;

        private readonly TournamentsComponent _tournaments;

        private readonly IMainConfig _mainConfig;

        private readonly DiscreteMessagesProvider _discreteMessagesProvider;

        public AdminPanel(ITelegramProvider telegramProvider, TournamentsComponent tournaments, IMainConfig config, PolyContext polyContext, ILogger logger, LichessClient client)
        {
            _polyContext = polyContext;
            _logger = logger;
            _lichessClient = client;
            _discreteMessagesProvider = new(telegramProvider);
            _mainConfig = config;
            _tournaments = tournaments;
        }

        [TelegramCommand("panel", "Выводит панель администратора", IsHidden = true, IsAdmin = true)]
        private async Task Panel(TelegramCommandExecutionContext ctx)
        {
            TelegramMessageBuilder message = "🛠 Добро пожаловать в панель администратора.";

            message.AddButton(
                new InlineKeyboardButton("🔄 Обновить турниры").WithData(nameof(UpdateTournaments))
            );

            message.AddButton(
                new InlineKeyboardButton("👥 Получить список студентов").WithData(nameof(GetStudentsList))
            );

            message.AddKeyboard(
            [
                new InlineKeyboardButton("➕ Добавить урок").WithData(nameof(AddLesson)),
                new InlineKeyboardButton("➕ Добавить посещение").WithData(nameof(AddAttendance)),
            ]);

            message.AddButton(
                new InlineKeyboardButton("➕ Добавить студентов").WithData(nameof(AddStudents))
            );

            message.AddButton(
                new InlineKeyboardButton("Поиск студента").WithData(nameof(SearchStudent))
            );

            await ctx.ReplyAsync(message);
        }

        [TelegramButton(nameof(GetStudentsList))]
        private async Task GetStudentsList(TelegramButtonExecutionContext ctx)
        {
            if (!_polyContext.Students.Any())
            {
                await ctx.ReplyAsync("Студенты отсутствуют!");
                return;
            }

            await ctx.ReplyAsync("Началась сборка таблицы, это может занять некоторое время...");

            List<string> csv = [string.Join(',', ["Имя", "Фамилия", "Отчество", "Институт", "Курс", "Номер зачётки", "Группа", "Ник", "Рапид", "Блиц"])];
            foreach (var student in _polyContext.Students)
            {
                var lichessName = "Аккаунт не привязан";
                var rapidRating = "Отсутсвует";
                var blitzRating = "Отсутсвует";
                if (!string.IsNullOrEmpty(student.LichessId))
                {
                    var lichess = await _lichessClient.GetUserAsync(student.LichessId);
                    if (lichess != null)
                    {
                        lichessName = lichess.Username;
                        if (lichess.Perfomance.TryGetValue("rapid", out var rapid))
                            rapidRating = rapid.Rating.ToString();
                        if (lichess.Perfomance.TryGetValue("blitz", out var blitz))
                            blitzRating = blitz.Rating.ToString();
                    }
                }
                var entry = string.Join(',',
                [
                    student.Name,
                    student.Surname,
                    student.Patronymic,
                    student.Institute,
                    student.Year.ToString(),
                    student.RecordBookId,
                    student.Group,
                    lichessName,
                    rapidRating,
                    blitzRating
                ]);
                csv.Add(entry);
            }

            TelegramMessageBuilder message = "Таблица со всеми участниками секции в базе";
            Directory.CreateDirectory("Temp");
            var tempFilePath = Path.Combine("Temp", "students.csv");
            var tempFile = File.Create(tempFilePath);
            using (var streamWriter = new StreamWriter(tempFile))
            {
                foreach (var entry in csv)
                    streamWriter.WriteLine(entry);
                streamWriter.Close();
            }
            using var stream = File.Open(tempFilePath, FileMode.Open);
            await ctx.ReplyAsync(message.WithFile(stream, "students.csv"));
        }

        [TelegramButton(nameof(AddLesson))]
        private async Task AddLesson(TelegramButtonExecutionContext ctx)
        {
            DiscreteMessage message = new(
                _discreteMessagesProvider,
                [
                    new TelegramMessageBuilder("Введите дату начала урока"),
                    new TelegramMessageBuilder("Введите дату конца урока"),
                    new TelegramMessageBuilder("Введите широту урока или введите -, чтобы взять стандартную"),
                    new TelegramMessageBuilder("Введите долготу урока или введите -, чтобы взять стандартную")
                ],
                HandleLessonsDataEntered
            );

            if (ctx.Query.Message != null)
                await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

            async Task HandleLessonsDataEntered(DiscreteMessageEnteredArgs args)
            {
                if (args.Responses.Length != 4)
                    return;

                if (!DateTime.TryParse(args.Responses[0].Text, out var startDate))
                {
                    await args.ReplyAsync("Ошибка! Неверный формат даты начала урока.");
                    return;
                }

                if (!DateTime.TryParse(args.Responses[1].Text, out var endDate))
                {
                    await args.ReplyAsync("Ошибка! Неверный формат даты конца урока.");
                    return;
                }

                float? latitude = default;
                float? longitude = default;

                var latitudeResponse = args.Responses[2].Text;
                if (!string.IsNullOrEmpty(latitudeResponse) && latitudeResponse != "-")
                {
                    if (!float.TryParse(latitudeResponse, out var givenLatitude))
                    {
                        await args.ReplyAsync("Ошибка! Неверный формат широты.");
                        return;
                    }
                    latitude = givenLatitude;
                }

                var longitudeResponse = args.Responses[3].Text;
                if (!string.IsNullOrEmpty(longitudeResponse) && longitudeResponse != "-")
                {
                    if (!float.TryParse(longitudeResponse, out var givenLongitude))
                    {
                        await args.ReplyAsync("Ошибка! Неверный формат долготы.");
                        return;
                    }
                    longitude = givenLongitude;
                }

                if (startDate >= endDate)
                {
                    await args.ReplyAsync("Ошибка! Дата начала идёт после даты конца.");
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
                await args.ReplyAsync($"Урок с <b>{startDate:g} до {endDate:g}</b> успешно добавлен!");
            }
        }

        [TelegramButton(nameof(AddAttendance))]
        private async Task AddAttendance(TelegramButtonExecutionContext ctx)
        {
            DiscreteMessage message = new(
                _discreteMessagesProvider,
                [
                    new TelegramMessageBuilder("Введите данные в формате списка Id урока,TelegramId студента")
                ],
                HandleAttendancesEntered
            );

            if (ctx.Query.Message != null)
                await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

            async Task HandleAttendancesEntered(DiscreteMessageEnteredArgs args)
            {
                List<string> errors = [];
                var response = args.Responses.First().Text;
                if (response == null)
                {
                    await args.ReplyAsync("Вы не ввели текст!");
                    return;
                }

                foreach (var entry in response.Split('\n'))
                {
                    var data = entry.Split(',');
                    if (data.Length != 2)
                    {
                        errors.Add($"Неверный формат данных: {entry}");
                        continue;
                    }

                    if (int.TryParse(data[0], out var lessonId) && long.TryParse(data[1], out var telegramId))
                    {
                        var student = _polyContext.Students.FirstOrDefault(s => s.TelegramId == telegramId);
                        var lesson = _polyContext.Lessons.FirstOrDefault(l => l.Id == lessonId);
                        if (student == null)
                        {
                            errors.Add($"Студент с TelegramId {telegramId} не найден!");
                            continue;
                        }
                        if (lesson == null)
                        {
                            errors.Add($"Урок с Id {lessonId} не найден!");
                            continue;
                        }

                        Attendance attendance = new()
                        {
                            Lesson = lesson,
                            Student = student
                        };
                        _polyContext.Attendances.Add(attendance);
                    }
                    else
                        errors.Add($"Ошибка при разборе данных: {entry}");
                }

                await _polyContext.SaveChangesAsync();
                if (errors.Count > 0)
                    await args.ReplyAsync($"Ошибки при добавлении:\n{string.Join('\n', errors)}");
                else
                    await args.ReplyAsync($"Посещения успешно добавлены!");
            }
        }

        [TelegramButton(nameof(UpdateTournaments))]
        private async Task UpdateTournaments(TelegramButtonExecutionContext ctx)
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

        [TelegramButton(nameof(AddStudents))]
        private async Task AddStudents(TelegramButtonExecutionContext ctx)
        {
            DiscreteMessage message = new(
                _discreteMessagesProvider,
                [
                    new TelegramMessageBuilder("Введите данные студентов в формате Имя,Фамилия,Отчество,Курс,Институт,Lichess,LichessToken,TelegramId")
                ],
                HandleStudentsEntered
            );

            if (ctx.Query.Message != null)
                await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

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
                    if (studentData.Length != 10)
                    {
                        skippedStudents.Add((student, "Не введены все данные (для пустых оставьте пропуск). Имя,Фамилия,Отчество,Курс,Группа,Институт,Номер зачётки,Личесс,ЛичессТокен,Телеграм"));
                        continue;
                    }

                    var surname = studentData[0];
                    var name = studentData[1];
                    var patronomic = studentData[2];
                    var yearStr = studentData[3];
                    var group = studentData[4];
                    var institute = studentData[5];
                    var recordBookId = studentData[6];
                    var lichess = studentData[7];
                    var lichessToken = studentData[8];
                    var telegramIdStr = studentData[9];

                    if (!int.TryParse(yearStr, out var year))
                    {
                        skippedStudents.Add((name, "Курс не является числом!"));
                        continue;
                    }

                    long telegramId = 0;
                    if (!string.IsNullOrEmpty(telegramIdStr) && !long.TryParse(telegramIdStr, out telegramId))
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
                        Group = group,
                        RecordBookId = string.IsNullOrEmpty(recordBookId) ? null : recordBookId,
                        TelegramId = telegramId == 0 ? default : telegramId,
                        LichessId = string.IsNullOrEmpty(lichess) ? null : lichess,
                        LichessToken = string.IsNullOrEmpty(lichessToken) ? null : lichessToken,
                        Institute = institute
                    };
                    _polyContext.Students.Add(studentEntry);
                }
                await _polyContext.SaveChangesAsync();
                await args.ReplyAsync($"Успешно добавлено {students.Length - skippedStudents.Count} студентов! Пропущенные студенты:\n{string.Join('\n', skippedStudents.Select(s => s.student + ": " + s.error))}");
            }
        }

        [TelegramButton(nameof(SearchStudent))]
        private async Task SearchStudent(TelegramButtonExecutionContext ctx)
        {
            DiscreteMessage message = new(
                _discreteMessagesProvider,
                [
                    new TelegramMessageBuilder("Введите имя студента или телеграм")
                ],
                HandleNameOrTelegramEntered
            );

            if (ctx.Query.Message != null)
                await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

            async Task HandleNameOrTelegramEntered(DiscreteMessageEnteredArgs args)
            {
                var response = args.Responses.First().Text;
                if (response == null)
                {
                    await args.ReplyAsync("Вы не ввели текст!");
                    return;
                }

                var foundedStudents = GetStudentByParameter(response);
                if (foundedStudents.Count == 0)
                {
                    await args.ReplyAsync("Студенты не найдены!");
                    return;
                }

                TelegramMessageBuilder replyMessage = $"Найдено {foundedStudents.Count} студентов:\n";
                foreach (var student in foundedStudents)
                    replyMessage.Text += student.ToString();

                await args.ReplyAsync(replyMessage);
            }

            List<Student> GetStudentByParameter(string parameter)
            {
                List<Student> foundedStudents = [];
                if (long.TryParse(parameter, out var telegramId))
                {
                    var studentByTelegram = _polyContext.Students.FirstOrDefault(s => s.TelegramId == telegramId);
                    if (studentByTelegram != null)
                        foundedStudents.Add(studentByTelegram);
                }
                else
                {
                    foreach (var student in _polyContext.Students)
                    {
                        if (student.Name + " " + student.Surname + " " + student.Patronymic == parameter)
                        {
                            foundedStudents.Add(student);
                            break;
                        }

                        if (student.Surname + " " + student.Name == parameter)
                        {
                            foundedStudents.Add(student);
                            continue;
                        }

                        if (student.Surname.Contains(parameter, StringComparison.CurrentCultureIgnoreCase) ||
                           student.Name.Contains(parameter, StringComparison.CurrentCultureIgnoreCase) ||
                           student.Patronymic.Contains(parameter, StringComparison.CurrentCultureIgnoreCase))
                        {
                            foundedStudents.Add(student);
                            continue;
                        }
                    }
                }

                return foundedStudents;
            }
        }
    }
}
