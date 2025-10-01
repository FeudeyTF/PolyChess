using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChess.Core.Telegram.Messages.Pagination.Builders
{
    internal class SimplePaginationMessageBuilder<TValue> : IPaginationMessageBuilder<TValue>
    {
        private readonly Func<TValue, int, string> _valueToString;

        public SimplePaginationMessageBuilder(Func<TValue, int, string> valueToString)
        {
            _valueToString = valueToString;
        }

        public ITelegramMessage Build(List<TValue> values, string type, int page, int totalPages)
        {
            var text = BuildMessageText(values, page, totalPages);
            var keyboard = BuildPaginationKeyboard(page, totalPages, type);
            return new TelegramMessageBuilder(text, replyMarkup: keyboard);
        }

        private string BuildMessageText(List<TValue> items, int page, int totalPages)
        {
            var text = string.Empty;

            if (items.Count == 0)
                return text + "Список пуст.";

            for (int i = 0; i < items.Count; i++)
                text += $"{_valueToString(items[i], i)}\n";

            if (totalPages > 1)
                text += $"📄 Страница {page + 1} из {totalPages}";

            return text;
        }

        private InlineKeyboardMarkup BuildPaginationKeyboard(int page, int totalPages, string type)
        {
            List<List<InlineKeyboardButton>> buttons = [];

            if (totalPages > 1)
            {
                List<InlineKeyboardButton> navigationButtons = [];

                if (page > 0)
                {
                    navigationButtons.Add(InlineKeyboardButton.WithCallbackData(
                        "⬅️ Назад",
                        PaginationCallbackData.Create(type, "prev", page - 1))
                    );
                }

                navigationButtons.Add(InlineKeyboardButton.WithCallbackData(
                    $"{page + 1}/{totalPages}",
                    PaginationCallbackData.Create(type, "current", page))
                );

                if (page < totalPages - 1)
                {
                    navigationButtons.Add(InlineKeyboardButton.WithCallbackData(
                        "Вперёд ➡️",
                        PaginationCallbackData.Create(type, "next", page + 1))
                    );
                }

                buttons.Add(navigationButtons);
            }

            return new InlineKeyboardMarkup(buttons);
        }
    }
}
