using Microsoft.Data.Sqlite;
using PolyChessTGBot.Logs;

namespace PolyChessTGBot.Database
{
    internal class PolyData
    {
        private readonly SqliteConnection MainDatabase;

        public PolyData(string path)
        {
            string sqlPath = Path.Combine(Environment.CurrentDirectory, path);
            var dirName = Path.GetDirectoryName(sqlPath);
            if(dirName != null)
                Directory.CreateDirectory(dirName);
            MainDatabase = new(string.Format("Data Source={0}", sqlPath));
            Program.Logger.Write($"Database {MainDatabase.Database} connected!", LogType.Info);
        }

        public int Query(string query, params object[] args)
        {
            using SqliteConnection db = Clone();
            db.Open();

            using SqliteCommand dbCommand = db.CreateCommand();
            dbCommand.CommandText = query;
            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine(6);
                AddParameter(dbCommand, "@" + i, args[i] ?? DBNull.Value);
            }

            return dbCommand.ExecuteNonQuery();
        }

        public QueryResult QueryReader(string query, params object[] args)
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
            => new() { ConnectionString = MainDatabase.ConnectionString};
    }
}
