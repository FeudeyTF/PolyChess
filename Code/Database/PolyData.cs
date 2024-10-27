using Microsoft.Data.Sqlite;

namespace PolyChessTGBot.Database
{
    internal class PolyData
    {
        public string DatabaseName => DB.Database;

        public List<User> Users;

        private readonly SqliteConnection DB;

        public PolyData(string path)
        {
            string sqlPath = Path.Combine(Environment.CurrentDirectory, path);
            var dirName = Path.GetDirectoryName(sqlPath);
            if (dirName != null)
                Directory.CreateDirectory(dirName);
            DB = new(string.Format("Data Source={0}", sqlPath));
            Users = GetAllUsers();
        }

        public void LoadTables()
        {
            Query("CREATE TABLE IF NOT EXISTS Users (" +
                  "TelegramID               INTEGER PRIMARY KEY, " +
                  "Name                     TEXT, " +
                  "LichessName              TEXT, " +
                  "Year                     INTEGER," +
                  "CreativeTaskCompleted    INT," +
                  "TokenKey                 TEXT" +
                  ")");
            Query("CREATE TABLE IF NOT EXISTS Attendance (" +
                  "LessonDate      INTEGER PRIMARY KEY, " +
                  "UserID          INTEGER" +
                  ")");
            Query("CREATE TABLE IF NOT EXISTS FAQ (" +
                  "ID              INTEGER PRIMARY KEY AUTOINCREMENT, " +
                  "Question        Text," +
                  "Answer          Text" +
                  ")");
            Query("CREATE TABLE IF NOT EXISTS HelpLinks (" +
                  "ID              INTEGER PRIMARY KEY AUTOINCREMENT, " +
                  "Title           Text," +
                  "Text            Text," +
                  "Footer          Text," +
                  "FileID          Text" +
                  ")");
        }

        public User? GetUser(long telegramID)
        {
            foreach (var user in Users)
                if (user.TelegramID == telegramID)
                    return user;
            return null;
        }

        private List<User> GetAllUsers()
        {
            List<User> result = [];
            using var reader = SelectQuery($"SELECT * FROM Users");
            while (reader.Read())
                result.Add(new(reader.Get<long>("TelegramID"), reader.Get("Name"), reader.Get("LichessName"), reader.Get<int>("Year"), reader.Get<int>("CreativeTaskCompleted"), reader.Get<string>("TokenKey")));
            return result;
        }

        public List<HelpLink> GetHelpLinks()
        {
            using var reader = SelectQuery("SELECT * FROM HelpLinks");
            List<HelpLink> links = [];
            while (reader.Read())
                links.Add(new(reader.Get<int>("ID"), reader.Get("Title"), reader.Get("Text"), reader.Get("Footer"), reader.Get("FileID")));
            return links;
        }

        public List<FAQEntry> GetFAQEntries()
        {
            using var reader = SelectQuery("SELECT * FROM FAQ");
            List<FAQEntry> questions = [];
            while (reader.Read())
                questions.Add(new(reader.Get<int>("ID"), reader.Get("Question"), reader.Get("Answer")));
            return questions;
        }

        public int Query(string query, params object[] args)
        {
            using SqliteConnection db = Clone();
            db.Open();

            using SqliteCommand dbCommand
                = db.CreateCommand();
            dbCommand.CommandText = query;
            for (int i = 0; i < args.Length; i++)
                AddParameter(dbCommand, "@" + i, args[i] ?? DBNull.Value);

            return dbCommand.ExecuteNonQuery();
        }

        public QueryResult SelectQuery(string query, params object[] args)
        {
            SqliteConnection db = Clone();
            db.Open();
            SqliteCommand command = db.CreateCommand();
            command.CommandText = query;
            for (int i = 0; i < args.Length; i++)
                AddParameter(command, "@" + i, args[i]);

            return new QueryResult(db, command.ExecuteReader(), command);
        }

        public T? QueryScalar<T>(string query, params object[] args)
        {
            using var db = Clone();
            db.Open();
            using var commad = db.CreateCommand();
            commad.CommandText = query;
            for (int i = 0; i < args.Length; i++)
                AddParameter(commad, "@" + i, args[i]);

            object? output = commad.ExecuteScalar();
            if (output != null && output.GetType() != typeof(T))
            {
                if (typeof(IConvertible).IsAssignableFrom(output.GetType()))
                {
                    return (T)Convert.ChangeType(output, typeof(T));
                }
            }

            return (T?)output;
        }

        private SqliteParameter AddParameter(SqliteCommand command, string name, object data)
        {
            SqliteParameter dbDataParameter = command.CreateParameter();
            dbDataParameter.ParameterName = name;
            dbDataParameter.Value = data;
            command.Parameters.Add(dbDataParameter);
            return dbDataParameter;
        }

        public SqliteConnection Clone()
            => new() { ConnectionString = DB.ConnectionString };
    }
}
