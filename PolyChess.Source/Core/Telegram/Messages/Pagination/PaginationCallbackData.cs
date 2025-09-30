namespace PolyChess.Core.Telegram.Messages.Pagination
{
    internal class PaginationCallbackData
    {
        public string Action { get; set; } = string.Empty;

        public int Page { get; set; }

        public string Type { get; set; } = string.Empty;

        public string? AdditionalData { get; set; }

        public static string Create(string type, string action, int page, string? additionalData = null)
        {
            var data = $"page_{type}_{action}_{page}";
            if (!string.IsNullOrEmpty(additionalData))
                data += $"_{additionalData}";
            return data;
        }

        public static PaginationCallbackData Parse(string callbackData)
        {
            var parts = callbackData.Split('_');

            if (parts.Length < 4 || parts[0] != "page")
                return new PaginationCallbackData();

            return new PaginationCallbackData
            {
                Type = parts[1],
                Action = parts[2],
                Page = int.TryParse(parts[3], out var page) ? page : 0,
                AdditionalData = parts.Length > 4 ? string.Join("_", parts[4..]) : null
            };
        }
    }
}
