using MySqlConnector;

namespace SharpTimer.Database
{
    public class MySqlDatabase : Database
    {
        public MySqlDatabase(string dbPath) : base(dbPath, DatabaseType.MySQL)
        {
        }       
    }
}