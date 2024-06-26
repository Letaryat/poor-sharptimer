using System.Text.Json;
using MySqlConnector;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Data;
using Npgsql;

namespace SharpTimer.Database
{
    public enum DatabaseType
    {
        MySQL,
        PostgreSQL
    }

    public abstract class Database
    {
        protected string connectionString;
        protected DatabaseType dbType;
        protected Database(string connectionString, DatabaseType dbType)
        {
            this.connectionString = connectionString;
            this.dbType = dbType;
        }

        public async Task<IDbConnection> OpenConnectionAsync(string connectionString)
        {
            IDbConnection connection = null;
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    connection = new MySqlConnection(connectionString);
                    await (connection as MySqlConnection).OpenAsync();
                    break;
                case DatabaseType.PostgreSQL:
                    connection = new NpgsqlConnection(connectionString);
                    await (connection as NpgsqlConnection).OpenAsync();
                    break;
            }
            return connection;
        }
    }
}