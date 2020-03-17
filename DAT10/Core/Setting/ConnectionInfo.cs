namespace DAT10.Core.Setting
{
    public class ConnectionInfo
    {
        /// <summary>
        /// String describing how to connect to a specific database
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Type of connection (e.g. SQL Server, Postgres, and CSV)
        /// </summary>
        public string ConnectionType { get; set; }

        public ConnectionInfo(string connectionString, string connectionType)
        {
            ConnectionString = connectionString;
            ConnectionType = connectionType;
        }

        /// <summary>
        /// For serialization
        /// </summary>
        private ConnectionInfo() { }
            
    }
}
