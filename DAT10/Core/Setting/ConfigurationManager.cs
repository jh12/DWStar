using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DAT10.Modules;
using Newtonsoft.Json;
using SimpleInjector;

namespace DAT10.Core.Setting
{
    /// <summary>
    /// Manager for configuration serialization
    /// </summary>
    public class ConfigurationManager
    {
        private readonly Container _container;
        private readonly JsonSerializerSettings _settings;
        
        /// <summary>
        /// Path to configuration folder
        /// </summary>
        public string ConfigurationPath { get; private set; }

        public ConfigurationManager(Container container, List<IModule> modules)
        {
            _container = container;

            _settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };

            // Add converter for modules
            _settings.Converters.Add(new ModuleTypeConverter(modules));

            ConfigurationPath = Path.Combine(ModuleEngine.ProgramPath, "Configurations");
        }

        /// <summary>
        /// Save configurations to files
        /// </summary>
        /// <param name="configurations">List of configurations</param>
        /// <returns>A task</returns>
        public async Task SaveConfigurations(List<Configuration> configurations)
        {
            // Check if directory already exists
            if (!Directory.Exists(ConfigurationPath))
                Directory.CreateDirectory(ConfigurationPath);

            foreach (var configuration in configurations)
            {
                var jsonString = JsonConvert.SerializeObject(configuration, _settings);

                // Check if configuration has changed name, if it has then change file name
                if (!configuration.Name.Equals(configuration.SavedName, StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(configuration.SavedName))
                    if (File.Exists(GetFile(configuration)))
                        File.Move(configuration.SavedName, configuration.Name);

                // Write configuration to file
                using (var writer = new StreamWriter(GetFile(configuration), false))
                {
                    await writer.WriteAsync(jsonString);
                }
            }
        }

        /// <summary>
        /// Load configurations
        /// </summary>
        /// <returns>List of configurations</returns>
        public async Task<List<Configuration>> LoadConfigurations()
        {
            if (!Directory.Exists(ConfigurationPath))
                return new List<Configuration> ();

            List<Configuration> configurations = new List<Configuration>();
            // Iterate through all files in directory
            foreach (var file in Directory.GetFiles(ConfigurationPath))
            {
                using (var stream = File.OpenRead(file))
                using (var reader = new StreamReader(stream))
                {
                    // Deserialize data in file and add to configurations list
                    var jsonString = await reader.ReadToEndAsync();

                    var deserializeObject = JsonConvert.DeserializeObject<Configuration>(jsonString, _settings);
                    configurations.Add(deserializeObject);
                }
            }

            return configurations;
        }

        /// <summary>
        /// Get file name for configuration
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private string GetFile(Configuration configuration)
        {
            return Path.Combine(ConfigurationPath, configuration.Name) + ".json";
        }
    }
}
