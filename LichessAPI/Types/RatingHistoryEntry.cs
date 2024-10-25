namespace LichessAPI.Types
{
    public class RatingHistoryEntry
    {
        public string Name = string.Empty;

        public List<RatingHistoryEntryPoint> Points = [];
    }

    //[JsonPropertyName(typeof(RatingHistoryEntryConverter))]
    public class RatingHistoryEntryPoint
    {
        public DateTime Date;

        public int Score;
    }
}