using Newtonsoft.Json;
using PolyChessTGBot.Lichess.Converters;

namespace PolyChessTGBot.Lichess.Types
{
    public class RatingHistoryEntry
    {
        public string Name = string.Empty;

        public List<RatingHistoryEntryPoint> Points = [];
    }

    [JsonConverter(typeof(RatingHistoryEntryConverter))]
    public class RatingHistoryEntryPoint
    {
        public DateTime Date;

        public int Score;
    }
}