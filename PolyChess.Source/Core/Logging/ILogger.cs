namespace PolyChess.Core.Logging
{
    internal interface ILogger
    {
        public void Write(string message, LogLevel logLevel);
    }
}
