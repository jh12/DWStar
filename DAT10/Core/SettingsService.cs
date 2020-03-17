using System.IO;
using System.Threading.Tasks;
using DAT10.Modules;
using Newtonsoft.Json;

namespace DAT10.Core
{
    public class SettingsService
    {
        private string CoreSettingsPath;

        /// <summary>
        /// Settings this to false will only get settings from code, at not from files
        /// </summary>
        private bool UseStoredSettings = false;

        public SettingsService()
        {
            CoreSettingsPath = Path.Combine(ModuleEngine.ProgramPath, "Settings");
        }

        public async Task<T> LoadSetting<T>(object @this, string filename = "") where T : new()
        {
            var path = GetPath<T>(@this, filename);

            if(!UseStoredSettings)
                return new T();

            if (!File.Exists(path))
            {
                var setting = new T();
                await SaveSetting(setting, @this, filename);
                return setting;
            }

            using (var stream = File.OpenRead(path))
            using (var reader = new StreamReader(stream))
            {
                var jsonString = await reader.ReadToEndAsync();

                return JsonConvert.DeserializeObject<T>(jsonString);
            }
        }

        public async Task SaveSetting<T>(T setting, object @this, string filename = "") where T : new()
        {
            var path = GetPath<T>(@this, filename);

            var json = JsonConvert.SerializeObject(setting);

            using (var writer = new StreamWriter(path))
            {
                await writer.WriteAsync(json);
            }
        }

        /// <summary>
        /// Get desired settings path. Create directory if not existent
        /// </summary>
        /// <typeparam name="T">Type of setting</typeparam>
        /// <param name="this">Object requesting setting</param>
        /// <param name="filename">Optional filename of setting</param>
        /// <returns>Path to file</returns>
        private string GetPath<T>(object @this, string filename) where T : new()
        {
            string path;

            // If module, then place together with DLL
            if (@this is IModule)
                path = ModuleEngine.ModulePath((IModule)@this);
            else
                path = CoreSettingsPath;

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return Path.Combine(path, (string.IsNullOrWhiteSpace(filename) ? typeof(T).Name : filename) + ".json");
        }
    }
}
