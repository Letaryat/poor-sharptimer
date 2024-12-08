using System.Data;
using System.Data.Common;

namespace SharpTimer
{
    public static class DbCommandExtensions
    {
        public static void AddParameterWithValue(this DbCommand command, string parameterName, object value)
        {
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }
        public static bool GetSQLiteBool(this DbDataReader reader, string columnName)
        {
            return reader.GetInt32(reader.GetOrdinal(columnName)) == 1;
        }
    }
}