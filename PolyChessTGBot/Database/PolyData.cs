using Microsoft.Data.Sqlite;

namespace PolyChessTGBot.Database
{
    internal class PolyData
    {
        public string DatabaseName => DB.Database;

        private readonly SqliteConnection DB;

        public PolyData(string path)
        {
            string sqlPath = Path.Combine(Environment.CurrentDirectory, path);
            var dirName = Path.GetDirectoryName(sqlPath);
            if (dirName != null)
                Directory.CreateDirectory(dirName);
            DB = new(string.Format("Data Source={0}", sqlPath));
        }

        public void LoadTables()
        {
            Query("CREATE TABLE IF NOT EXISTS Users (" +
                  "TelegramID      INTEGER PRIMARY KEY, " +
                  "Name            TEXT, " +
                  "Year            INTEGER" +
                  ")");
            Query("CREATE TABLE IF NOT EXISTS Attendance (" +
                  "LessonDate      INTEGER PRIMARY KEY, " +
                  "UserID          INTEGER" +
                  ")");
            Query("CREATE TABLE IF NOT EXISTS QnA (" +
                  "ID              INTEGER PRIMARY KEY AUTOINCREMENT, " +
                  "Question        Text," +
                  "Answer          Text" +
                  ")");
        }

        public int Query(string query, params object[] args)
        {
            using SqliteConnection db = Clone();
            db.Open();

            using SqliteCommand dbCommand = db.CreateCommand();
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

    public struct QnAEntry
    {
        public string Question;

        public string Answer;

        public QnAEntry(string question, string answer)
        {
            Question = question;
            Answer = answer;
        }
    }
}
