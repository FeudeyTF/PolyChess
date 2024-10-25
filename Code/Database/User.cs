namespace PolyChessTGBot.Database
{
    public class User(long telegramID, string name, string lichessName, long year, int creativeTaskCompleted)
    {
        public long TelegramID = telegramID;

        public string Name = name;

        public string LichessName = lichessName;

        public long Year = year;

        public bool CreativeTaskCompleted = creativeTaskCompleted != 0;

        public override string ToString()
        {
            return $"{Name} '{LichessName}' ({TelegramID}), Курс - {Year}";
        }
    }
}
