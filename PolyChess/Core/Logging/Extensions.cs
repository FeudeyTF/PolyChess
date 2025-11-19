namespace PolyChess.Core.Logging
{
    internal static class LoggerExtensions
    {
        public static void Info(this ILogger logger, string message)
            => logger.Write(message, LogLevel.Info);

        public static void Warn(this ILogger logger, string message)
            => logger.Write(message, LogLevel.Warn);

        public static void Error(this ILogger logger, string message)
            => logger.Write(message, LogLevel.Error);

        public static void Debug(this ILogger logger, string message)
            => logger.Write(message, LogLevel.Debug);

        public static void Info(this ILogger logger, string format, params object[] args)
            => logger.Write(string.Format(format, args), LogLevel.Info);

        public static void Warn(this ILogger logger, string format, params object[] args)
            => logger.Write(string.Format(format, args), LogLevel.Warn);

        public static void Error(this ILogger logger, string format, params object[] args)
            => logger.Write(string.Format(format, args), LogLevel.Error);

        public static void Debug(this ILogger logger, string format, params object[] args)
            => logger.Write(string.Format(format, args), LogLevel.Debug);

        public static void Error(this ILogger logger, Exception e)
            => logger.Error(e.ToString());
    }
}
