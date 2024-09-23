namespace PolyChessTGBot.Logs.LogTypes
{
    public class TextLog : ILog, IDisposable
    {
        public string Filename { get; }

        private StreamWriter? LogWriter;

        public TextLog(string filename, string folder)
        {
            string path = Path.Combine(Environment.CurrentDirectory, folder);
            string filePath = Path.Combine(path, filename);
            if(!Directory.Exists(path))
                Directory.CreateDirectory(path);
            if (!File.Exists(filePath))
                File.Create(filePath).Close();
            Filename = filePath;
        }

        public void Write(string message, LogType type)
        {
            LogWriter ??= new(Filename);
            string logEntry = type.ToString().ToUpper() + ": " + message;

            Console.ForegroundColor = GetColor(type);
            Console.WriteLine(logEntry);
            Console.ResetColor();

            LogWriter.WriteLine(logEntry);
		    LogWriter.Flush();
        }

        private ConsoleColor GetColor(LogType log)
        {
            return log switch
            {
                LogType.Debug => ConsoleColor.DarkYellow,
                LogType.Error => ConsoleColor.Red,
                LogType.Warn => ConsoleColor.Yellow,
                LogType.Info => ConsoleColor.Yellow,
                _ => ConsoleColor.White
            };
        }

		public void Dispose()
		{
			LogWriter?.Dispose();
            GC.SuppressFinalize(this);
		}
    }
}