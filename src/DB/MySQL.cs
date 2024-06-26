using MySqlConnector;

namespace SharpTimer.Database
{
    public class MySqlDatabase : Database
    {
        public MySqlDatabase(string connectionString) : base(connectionString, DatabaseType.MySQL)
        {
        }       
    }
}