using System.Data;

namespace PolyChessTGBot.Database
{
    public class QueryResult : IDisposable
    {
        private static readonly Dictionary<Type, Func<IDataReader, int, object?>> ReadFuncs = new Dictionary
            <Type, Func<IDataReader, int, object?>>
        {
            {
                typeof (bool),
                (s, i) => s.GetBoolean(i)
            },
            {
                typeof (bool?),
                (s, i) => s.IsDBNull(i) ? null : s.GetBoolean(i)
            },
            {
                typeof (byte),
                (s, i) => s.GetByte(i)
            },
            {
                typeof (byte?),
                (s, i) => s.IsDBNull(i) ? null : s.GetByte(i)
            },
            {
                typeof (Int16),
                (s, i) => s.GetInt16(i)
            },
            {
                typeof (Int16?),
                (s, i) => s.IsDBNull(i) ? null : s.GetInt16(i)
            },
            {
                typeof (Int32),
                (s, i) => s.GetInt32(i)
            },
            {
                typeof (Int32?),
                (s, i) => s.IsDBNull(i) ? null : s.GetInt32(i)
            },
            {
                typeof (Int64),
                (s, i) => s.GetInt64(i)
            },
            {
                typeof (Int64?),
                (s, i) => s.IsDBNull(i) ? null : s.GetInt64(i)
            },
            {
                typeof (string),
                (s, i) => s.GetString(i)
            },
            {
                typeof (decimal),
                (s, i) => s.GetDecimal(i)
            },
            {
                typeof (decimal?),
                (s, i) => s.IsDBNull(i) ? null : s.GetDecimal(i)
            },
            {
                typeof (float),
                (s, i) => s.GetFloat(i)
            },
            {
                typeof (float?),
                (s, i) => s.IsDBNull(i) ? null : s.GetFloat(i)
            },
            {
                typeof (double),
                (s, i) => s.GetDouble(i)
            },
            {
                typeof (double?),
                (s, i) => s.IsDBNull(i) ? null : s.GetDouble(i)
            },
            {
                typeof (DateTime),
                (s, i) => s.IsDBNull(i) ? null : s.GetDateTime(i)
            },
            {
                typeof (object),
                (s, i) => s.GetValue(i)
            },
        };

        public IDbConnection? Connection { get; protected set; }

        public IDataReader? Reader { get; protected set; }

        public IDbCommand? Command { get; protected set; }

        public QueryResult(IDbConnection connection, IDataReader reader, IDbCommand command)
        {
            Connection = connection;
            Reader = reader;
            Command = command;
        }

        ~QueryResult()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Reader != null)
                {
                    Reader.Dispose();
                    Reader = null;
                }
                if (Command != null)
                {
                    Command.Dispose();
                    Command = null;
                }
                if (Connection != null)
                {
                    Connection.Dispose();
                    Connection = null;
                }
            }
        }

        public bool Read()
        {
            if (Reader == null)
                return false;
            return Reader.Read();
        }

        public T? Get<T>(string column)
        {
            if (Reader == null)
                return default;
            return Get<T>(Reader.GetOrdinal(column));
        }

        public string Get(string column)
        {
            if (Reader == null)
                return string.Empty;
            string? r = Get<string>(Reader.GetOrdinal(column));
            if (r == null)
                return string.Empty;
            return r;
        }

        public T? Get<T>(int column)
        {
            if (Reader == null)
                return default;

            if (Reader.IsDBNull(column))
                return default;

            if (ReadFuncs.ContainsKey(typeof(T)))
                return (T?)ReadFuncs[typeof(T)](Reader, column);

            if (Reader.IsDBNull(column))
            {
                return default;
            }

            return (T)Reader.GetValue(column);
        }
    }
}
