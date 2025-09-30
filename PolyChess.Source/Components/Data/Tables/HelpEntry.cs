namespace PolyChess.Components.Data.Tables
{
    internal class HelpEntry
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public string Footer { get; set; } = string.Empty;

        public string? FileId { get; set; } = string.Empty;
    }
}
