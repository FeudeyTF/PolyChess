namespace PolyChessTGBot.Database
{
    public class User(long telegramID, string name, string lichessName, long year, int creativeTaskCompleted, string? tokenKey)
    {
        public long TelegramID = telegramID;

        public string Name = name;

        public string LichessName = lichessName;

        public long Year = year;

        public bool CreativeTaskCompleted = creativeTaskCompleted != 0;

        public string? TokenKey = tokenKey;

        public override string ToString()
        {
            return $"Имя: <b>{Name}</b>, Ник: <b>{LichessName}</b>, TID: <b>{TelegramID}</b>, Курс: <b>{Year}</b>";
        }
    }
}
