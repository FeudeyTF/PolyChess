namespace PolyChess.Core.Logging.Types
{
    internal class ConsoleLogger : ILogger
    {
        public void Write(string message, LogLevel logLevel)
        {
            ConsoleColor color = logLevel switch
            {
                LogLevel.Debug => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Warn => ConsoleColor.Red | ConsoleColor.Yellow,
                LogLevel.Info => ConsoleColor.Green,
                _ => ConsoleColor.White

            };
            Console.ForegroundColor = color;
            Console.WriteLine(logLevel.ToString().ToUpper() + ": " + message);
            Console.ResetColor();
        }
    }
}
