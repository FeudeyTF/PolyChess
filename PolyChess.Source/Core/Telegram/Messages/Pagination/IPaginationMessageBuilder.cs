namespace PolyChess.Core.Telegram.Messages.Pagination
{
    internal interface IPaginationMessageBuilder<TValue>
    {
        public ITelegramMessage Build(List<TValue> values, string type, int page, int totalPages);
    }
}
