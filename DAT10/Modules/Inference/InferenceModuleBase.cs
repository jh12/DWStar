using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAT10.Metadata.Model;

namespace DAT10.Modules.Inference
{
    public abstract class InferenceModuleBase : IModule
    {
        protected DataSampleService SampleService { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }

        protected InferenceModuleBase(DataSampleService sampleService)
        {
            SampleService = sampleService;
        }

        /// <summary>
        /// Check if connection is valid
        /// </summary>
        /// <param name="connection">String defining the connection (e.g. OLEDB connection, file path)</param>
        /// <returns>True if correctly formatted string, otherwise false</returns>
        public abstract bool IsValidConnection(string connection);

        public CommonModel GetSchema(string connectionstring)
        {
            var commonModel = GetSchemaForConnection(connectionstring);

            var databases = commonModel.Tables.Select(t => t.Database).Distinct();

            // Register databases with sample service and set connection string
            foreach (var database in databases)
            {
                SampleService.RegisterDatabase(database, this);

                database.ConnectionString = connectionstring;
            }

            return commonModel;
        }

        /// <summary>
        /// Get metadata related to the connection
        /// </summary>
        /// <returns>Database</returns>
        protected abstract CommonModel GetSchemaForConnection(string connectionstring);

        public abstract Task<List<string>> GetDataSampleAsync(Column column, int amount = 100);

        public abstract Task<List<string[]>> GetDataSampleAsync(int amount = 100, params Column[] columns);

        /// <summary>
        /// Retrieve a supported source type (e.g. SQL Server)
        /// </summary>
        /// <returns>Supported source type</returns>
        public abstract string SupportedSourceType();
    }
}
