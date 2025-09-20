using Microsoft.Data.Sqlite;
namespace PolyChessTGBot.Database
{
    internal class PolyData
    {
        public string DatabaseName => DB.Database;

        public List<User> Users;

        public List<Event> Events;

        public List<Attendance> Attendances;

        public Dictionary<int, Lesson> Lessons;

        private readonly SqliteConnection DB;

        public PolyData(string path)
        {
            string sqlPath = Path.Combine(Environment.CurrentDirectory, path);
            var dirName = Path.GetDirectoryName(sqlPath);
            if (dirName != null)
                Directory.CreateDirectory(dirName);
            DB = new(string.Format("Data Source={0}", sqlPath));
            Users = [];
            Events = [];
            Attendances = [];
            Lessons = [];
        }

        public void Initialize()
        {
            LoadTables();
            Users = GetAllUsers();
            Events = GetAllEvents();
            Lessons = GetLessons();
            Attendances = GetAttendance();
        }

        public void LoadTables()
        {
            Query("CREATE TABLE IF NOT EXISTS Users (" +
                  "TelegramID               INTEGER PRIMARY KEY, " +
                  "Name                     TEXT, " +
                  "LichessName              TEXT, " +
                  "Year                     INTEGER," +
                  "CreativeTaskCompleted    INT DEFAULT 0," +
                  "TokenKey                 TEXT," +
                  "OtherTournaments         INT DEFAULT 0" +
                  ")");
            Query("CREATE TABLE IF NOT EXISTS Attendance (" +
                  "LessonID              INTEGER, " +
                  "UserID                INTEGER" +
                  ")");
            Query("CREATE TABLE IF NOT EXISTS Lessons (" +
                  "ID              INTEGER PRIMARY KEY AUTOINCREMENT, " +
                  "LessonDate      INTEGER" +
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
            Query("CREATE TABLE IF NOT EXISTS Events (" +
                  "Name            TEXT," +
                  "Description      TEXT," +
                  "Start           NUMERIC," +
                  "End             NUMERIC" +
                  ")");
        }

        public void InsertEvent(Event e)
        {
            Query($"INSERT INTO Events (Name, Description, Start, End) VALUES ('{e.Name}', '{e.Description}', '{e.Start.Ticks}', '{e.End.Ticks}')");
            Events.Add(e);
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
                result.Add(new(reader.Get<long>("TelegramID"), reader.Get("Name"), reader.Get("LichessName"), reader.Get<int>("Year"), reader.Get<int>("CreativeTaskCompleted"), reader.Get<string>("TokenKey"), reader.Get<int>("OtherTournaments")));
            return result;
        }

        private List<Event> GetAllEvents()
        {
            List<Event> result = [];
            using var reader = SelectQuery($"SELECT * FROM Events");
            while (reader.Read())
                result.Add(new(reader.Get("Name"), reader.Get("Description"), new DateTime(reader.Get<long>("Start")), new DateTime(reader.Get<long>("End"))));
            return result;
        }

        private List<Attendance> GetAttendance()
        {
            List<Attendance> result = [];
            using var reader = SelectQuery($"SELECT * FROM Attendance");
            while (reader.Read())
            {
                var user = GetUser(reader.Get<long>("UserID"));
                if (user != null)
                {
                    if (Lessons.TryGetValue(reader.Get<int>("LessonID"), out var lesson))
                        result.Add(new Attendance(user, lesson));
                }
            }
            return result;
        }

        private Dictionary<int, Lesson> GetLessons()
        {
            Dictionary<int, Lesson> result = [];
            using var reader = SelectQuery($"SELECT * FROM Lessons");
            while (reader.Read())
                result.Add(reader.Get<int>("ID"), new Lesson(reader.Get<int>("ID"), new DateTime(reader.Get<long>("LessonDate"))));
            return result;
        }

        public void AddLesson(DateTime date)
        {
            Query($"INSERT INTO Lessons (LessonDate) VALUES ('{date.Ticks}')");
            int id = QueryScalar<int>("SELECT last_insert_rowid()");
            Lessons.Add(id, new Lesson(id, date));
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

        public List<Attendance> GetUserAttendance(long telegramId)
        {
            List<Attendance> result = [];
            foreach (var attendance in Attendances)
                if (telegramId == attendance.User.TelegramID)
                    result.Add(attendance);
            return result;
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

        public void AddUserAttendance(User user, Lesson lesson)
        {
            Query($"INSERT INTO Attendance (UserID, LessonID) VALUES ('{user.TelegramID}', '{lesson.ID}')");
            Attendances.Add(new(user, lesson));
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
