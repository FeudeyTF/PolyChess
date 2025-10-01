using PolyChess.Components.Data;
using PolyChess.Components.Data.Tables;
using PolyChess.Components.Telegram.Commands;
using PolyChess.Components.Tournaments;
using PolyChess.Configuration;
using PolyChess.Core.Telegram.Messages;

namespace PolyChess.Components.Telegram.ClientCommands
{
    internal class AdminCommands : TelegramCommandAggregator
    {
        private readonly PolyContext _polyContext;

        private readonly TournamentsComponent _tournaments;

        private readonly IMainConfig _mainConfig;

        public AdminCommands(PolyContext polyContext, TournamentsComponent tournaments, IMainConfig config)
        {
            _polyContext = polyContext;
            _tournaments = tournaments;
            _mainConfig = config;
        }

        [TelegramCommand("addlesson", "Добавляет урок", IsAdmin = true, IsHidden = true)]
        private async Task AddLesson(TelegramCommandExecutionContext ctx, DateTime startDate, DateTime endDate)
        {
            Lesson lesson = new()
            {
                StartDate = startDate,
                EndDate = endDate
            };

            _polyContext.Lessons.Add(lesson);
            await _polyContext.SaveChangesAsync();
            await ctx.SendMessageAsync(new TelegramMessageBuilder("Урок в <b>{date}</b> успешно добавлен!"), ctx.Message.Chat.Id);
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
    }
}
