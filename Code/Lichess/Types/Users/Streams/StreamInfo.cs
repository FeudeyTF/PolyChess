namespace PolyChessTGBot.Lichess.Types
{
    public class StreamInfo
    {
        public string Name = string.Empty;

        public string ID = string.Empty;

        public Stream Stream = new();

        public Streamer Streamer = new();
    }
}
