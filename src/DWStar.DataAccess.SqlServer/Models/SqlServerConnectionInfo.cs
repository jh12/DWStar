using DWStar.DataAccess.Models;

namespace DWStar.DataAccess.SqlServer.Models
{
    public class SqlServerConnectionString : ConnectionInfo
    {
        public string ConnectionString { get; }

        public SqlServerConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}