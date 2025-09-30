using PolyChess.Components.Data;
using PolyChess.Components.Data.Tables;
using PolyChess.Configuration;
using PolyChess.Core.Telegram;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChess.Components.Telegram.Handlers
{
    internal class AttendanceHandler : ITelegramUpdateHandler
    {
        public UpdateType Type => UpdateType.Message;

        private readonly IMainConfig _mainConfig;

        private readonly PolyContext _polyContext;

        public AttendanceHandler(IMainConfig config, PolyContext context)
        {
            _mainConfig = config;
            _polyContext = context;
        }

        public async Task<bool> HandleUpdate(ITelegramBotClient client, Update update, CancellationToken token)
        {
            var message = update.Message!;
            var user = message.From;
            if (user == null)
                return false;

            if (message.Location != null)
            {
                if (message.Location.LivePeriod == null)
                {
                    await client.SendMessageAsync("Для того, чтобы отметиться на уроке, нужно транслировать геопозицию, а не просто отправить!", message.Chat.Id, token);
                    return true;
                }

                var distance = GetDistance(message.Location, new()
                {
                    Latitude = _mainConfig.SchoolLocation.X,
                    Longitude = _mainConfig.SchoolLocation.Y
                });

                if (distance > 0.3)
                {
                    await client.SendMessageAsync("Вы не на уроке!", message.Chat.Id, token);
                    return true;
                }

                var currentDate = DateTime.Now;
                var currentLesson = _polyContext.Lessons.FirstOrDefault(
                    l => currentDate >= l.StartDate && currentDate <= l.EndDate
                );

                if (currentLesson == null)
                {
                    await client.SendMessageAsync("Сегодня нет урока!", message.Chat.Id, token);
                    return true;
                }

                if (currentDate < currentLesson.StartDate || currentDate > currentLesson.EndDate)
                {
                    await client.SendMessageAsync($"Ещё не время урока! Урок будет с {currentLesson.StartDate:t} до {currentLesson.EndDate:t}", message.Chat.Id, token);
                    return true;
                }

                if (_polyContext.Attendances.Any(a => a.Student.TelegramId == user.Id && a.Lesson.Id == currentLesson.Id))
                {
                    await client.SendMessageAsync("Вы уже отметились на уроке!", message.Chat.Id, token);
                    return true;
                }

                var student = _polyContext.Students.FirstOrDefault(s => s.TelegramId == user.Id);
                if (student == null)
                {
                    await client.SendMessageAsync("Вы не являетесь участником секции!", message.Chat.Id, token);
                    return true;
                }

                _polyContext.Attendances.Add(new Attendance()
                {
                    Lesson = currentLesson,
                    Student = student
                });

                await _polyContext.SaveChangesAsync(token);
                await client.SendMessageAsync("Вы успешно отмечены на уроке!", message.Chat.Id, token);
            }
            return false;
        }

        private static double GetDistance(Location location1, Location location2)
        {
            var delta = Math.Acos(
                Math.Sin(location1.Latitude) * Math.Sin(location2.Latitude) +
                Math.Cos(location1.Latitude) * Math.Cos(location2.Latitude) *
                Math.Cos(location1.Longitude - location2.Longitude)
            );
            const double EarthArc = 111.1;

            return delta * EarthArc;
        }
    }
}
