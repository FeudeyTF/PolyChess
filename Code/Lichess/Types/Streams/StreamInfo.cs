namespace PolyChessTGBot.Lichess.Types.Streams
{
    public class StreamInfo
    {
        public string Name = string.Empty;

        public string ID = string.Empty;

        public Stream Stream = new();

        public Streamer Streamer = new();
    }
}
