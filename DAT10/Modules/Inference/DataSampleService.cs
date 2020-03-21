using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using DAT10.Metadata.Model;

namespace DAT10.Modules.Inference
{
    /// <summary>
    /// Used to associate a specific database with its inference module.
    /// Can then provide data samples from that specific database
    /// </summary>
    // TODO: Caching of data samples?
    public class DataSampleService
    {
        private Dictionary<Database, InferenceModuleBase> _associatedInferenceModules;

        public DataSampleService()
        {
            _associatedInferenceModules = new Dictionary<Database, InferenceModuleBase>();
        }

        /// <summary>
        /// Register a database in the service
        /// </summary>
        /// <param name="database">Database to register</param>
        /// <param name="inferenceModule">Module that is responsible of getting the data</param>
        public void RegisterDatabase(Database database, InferenceModuleBase inferenceModule)
        {
            if(_associatedInferenceModules.ContainsKey(database))
                throw new Exception("Database already has a module associated.");

            _associatedInferenceModules[database] = inferenceModule;
        }

        /// <summary>
        /// Get a datasample for multiple columns
        /// </summary>
        /// <param name="columns">Columns to retrieve data from</param>
        /// <returns>List of string array</returns>
        public async Task<List<string[]>> GetDataSampleAsync(params Column[] columns)
        {
            return await GetDataSampleAsync(100, columns);
        }

        /// <summary>
        /// Get a datasample for multiple columns
        /// </summary>
        /// <param name="amount">Amount of rows to return</param>
        /// <param name="columns">Columns to retrieve data from</param>
        /// <returns>List of string array</returns>
        public async Task<List<string[]>> GetDataSampleAsync(int amount, params Column[] columns)
        {
            if(columns.Select(c => c.Table.Database).Distinct().Count() > 1)
                throw new NoCommonDatabaseException("Not all columns originates from the same database.");

            return await _associatedInferenceModules[columns[0].Table.Database].GetDataSampleAsync(amount, columns);
        }

        /// <summary>
        /// Get a datasample for one column
        /// </summary>
        /// <param name="column">Column to retrieve data from</param>
        /// <returns>List of strings</returns>
        public async Task<List<string>> GetDataSampleAsync(Column column)
        {
            return await _associatedInferenceModules[column.Table.Database].GetDataSampleAsync(column);
        }

        /// <summary>
        /// Get a datasample for one column
        /// </summary>
        /// <param name="column">Column to retrieve data from</param>
        /// <param name="amount">Amount of rows to retrieve</param>
        /// <returns>List of strings</returns>
        public async Task<List<string>> GetDataSampleAsync(Column column, int amount)
        {
            return await _associatedInferenceModules[column.Table.Database].GetDataSampleAsync(column, amount);
        }

        public void ResetService()
        {
            _associatedInferenceModules.Clear();
        }
    }

    /// <summary>
    /// Called when multiple columns does not originate from the same database
    /// </summary>
    [Serializable]
    public class NoCommonDatabaseException : Exception
    {
        public NoCommonDatabaseException(string message) : base(message)
        {
        }

        protected NoCommonDatabaseException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
