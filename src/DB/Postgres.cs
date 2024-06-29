using Npgsql;

namespace SharpTimer.Database
{
    public class PostgreSqlDatabase : Database
    {
        public PostgreSqlDatabase(string dbPath) : base(dbPath, DatabaseType.PostgreSQL)
        {
        }
    }
}