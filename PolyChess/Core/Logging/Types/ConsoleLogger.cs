namespace PolyChess.Core.Logging.Types
{
    internal class ConsoleLogger : ILogger
    {
        public void Write(string message, LogLevel logLevel)
        {
            Console.WriteLine(logLevel.ToString().ToUpper() + ": " + message);
        }
    }
}
