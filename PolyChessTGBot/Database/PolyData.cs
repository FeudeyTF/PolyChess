using Microsoft.Data.Sqlite;
using PolyChessTGBot.Logs;
using System.Data;

namespace PolyChessTGBot.Database
{
    internal class PolyData
    {
        private readonly IDbConnection MainDatabase;

        public PolyData(string path)
        {
            string sqlPath = Path.Combine(Environment.CurrentDirectory, path);
            var dirName = Path.GetDirectoryName(sqlPath);
            if(dirName != null)
                Directory.CreateDirectory(dirName);
            MainDatabase = new SqliteConnection(string.Format("Data Source={0}", sqlPath));
            Program.Logger.Write($"Database {MainDatabase.Database} at {sqlPath} connected!", LogType.Info);
        }

        public int Query(string query, params object[] args)
        {
            using IDbConnection dbConnection = Clone(MainDatabase);
            dbConnection.Open();

            using IDbCommand dbCommand = dbConnection.CreateCommand();
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
            Console.WriteLine(1);
            IDbConnection dbConnection = Clone(MainDatabase);
            Console.WriteLine(2);
            dbConnection.Open();
            Console.WriteLine(3);
            IDbCommand dbCommand = dbConnection.CreateCommand();
            Console.WriteLine(4);
            dbCommand.CommandText = query;
            Console.WriteLine(5);
            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine(6);
                AddParameter(dbCommand, "@" + i, args[i]);
            }
            Console.WriteLine(7);

            return new QueryResult(dbConnection, dbCommand.ExecuteReader(), dbCommand);
        }

        public IDbDataParameter AddParameter(IDbCommand command, string name, object data)
        {
            IDbDataParameter dbDataParameter = command.CreateParameter();
            dbDataParameter.ParameterName = name;
            dbDataParameter.Value = data;
            command.Parameters.Add(dbDataParameter);
            return dbDataParameter;
        }

        public IDbConnection Clone(IDbConnection conn)
        {
            var instance = Activator.CreateInstance(conn.GetType());
            if (instance != null) // На самом деле всегда TRUE
            {
                IDbConnection db = (IDbConnection)instance;
                db.ConnectionString = conn.ConnectionString;
                return db;
            }
            return conn;
        }
    }
}
