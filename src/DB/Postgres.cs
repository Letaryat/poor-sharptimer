using Npgsql;

namespace SharpTimer.Database
{
    public class PostgreSqlDatabase : Database
    {
        public PostgreSqlDatabase(string connectionString) : base(connectionString, DatabaseType.PostgreSQL)
        {
        }
    }
}