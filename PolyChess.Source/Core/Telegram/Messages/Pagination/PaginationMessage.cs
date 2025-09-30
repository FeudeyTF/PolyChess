using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChess.Core.Telegram.Messages.Pagination
{
    internal class PaginationMessage<TValue> : ITelegramMessage
    {
        public Func<IEnumerable<TValue>> ItemsRetriever;

        public int ItemsPerPage;

        public string Type;

        private readonly IPaginationMessageBuilder<TValue> _messageBuilder;

        private readonly ITelegramProvider _telegramProvider;

        public PaginationMessage(string type, int itemsPerPage, Func<IEnumerable<TValue>> itemsRetriever, IPaginationMessageBuilder<TValue> builder, ITelegramProvider provider)
        {
            Type = type;
            ItemsRetriever = itemsRetriever;
            ItemsPerPage = itemsPerPage;
            _messageBuilder = builder;
            _telegramProvider = provider;
            _telegramProvider.OnCallback += HandleTelegramCallback;
        }

        public async Task SendAsync(ITelegramBotClient client, ChatId chatId, CancellationToken token)
        {
            List<TValue> itemsList = [.. ItemsRetriever()];
            var totalItems = itemsList.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / ItemsPerPage);

            var pageItems = GetPageValues(itemsList, 0);

            await _messageBuilder.Build(pageItems, Type, 0, totalPages).SendAsync(client, chatId, token);

        }

        public Task EditAsync(ITelegramBotClient client, Message oldMessage, CancellationToken token)
        {
            throw new NotImplementedException("Pagination message doesn't implements edit method"); ;
        }

        public async Task EditAsync(ITelegramBotClient client, Message oldMessage, int page, CancellationToken token)
        {
            List<TValue> itemsList = [.. ItemsRetriever()];
            var totalItems = itemsList.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / ItemsPerPage);

            page = Math.Max(0, Math.Min(page, totalPages - 1));

            var pageItems = GetPageValues(itemsList, page);

            await _messageBuilder.Build(pageItems, Type, page, totalPages).EditAsync(client, oldMessage, token);
        }

        private async Task HandleTelegramCallback(ITelegramBotClient client, CallbackQuery query, CancellationToken token)
        {
            if (query.Message == null)
                return;
            if (query.Data != null && query.Data.StartsWith("page_"))
            {
                var data = PaginationCallbackData.Parse(query.Data);
                if (data.Type == Type)
                    await EditAsync(client, query.Message, data.Page, token);
            }
        }

        private List<TValue> GetPageValues(List<TValue> values, int page)
        {
            var startIndex = page * ItemsPerPage;
            return [.. values.Skip(startIndex).Take(ItemsPerPage)];
        }
    }
}
