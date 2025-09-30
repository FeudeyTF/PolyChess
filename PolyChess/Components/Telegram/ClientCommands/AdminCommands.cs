using PolyChess.Components.Data;
using PolyChess.Components.Data.Tables;
using PolyChess.Components.Telegram.Commands;
using PolyChess.Core.Telegram.Messages;

namespace PolyChess.Components.Telegram.ClientCommands
{
    internal class AdminCommands : TelegramCommandAggregator
    {
        private readonly PolyContext _polyContext;

        public AdminCommands(PolyContext polyContext)
        {
            _polyContext = polyContext;
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
    }
}
