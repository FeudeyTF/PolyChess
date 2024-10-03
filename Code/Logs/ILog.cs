namespace PolyChessTGBot.Logs
{
    public interface ILog
    {
        public string Filename { get; }

        public void Write(string message, LogType type);
    }

    public enum LogType
    {
        Debug,
        Error,
        Warn,
        Info
    }
}