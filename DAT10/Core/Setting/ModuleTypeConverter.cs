using System;
using System.Collections.Generic;
using System.Linq;
using DAT10.Modules;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleLogger;

namespace DAT10.Core.Setting
{
    /// <summary>
    /// Converter for modules
    /// </summary>
    public class ModuleTypeConverter : JsonConverter
    {
        private readonly List<IModule> _modules;

        public ModuleTypeConverter(List<IModule> modules)
        {
            _modules = modules;
        }

        /// <summary>
        /// Serialization of module
        /// </summary>
        /// <param name="writer">JsonWriter</param>
        /// <param name="value">Value to serialize</param>
        /// <param name="serializer">Used serializer</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Write only the type of the module
            var jObject = JObject.FromObject(value);
            jObject.Add("Type", value.GetType().FullName);
            jObject.WriteTo(writer);
        }

        /// <summary>
        /// Deserialize json to module
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns>Module</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Get type of module
            var jObject = JObject.Load(reader);
            
            var type = Type.GetType(jObject.GetValue("Type").Value<string>());

            try
            {
                // Return module that matches this type
                return _modules.First(m => m.GetType() == type);
            }
            catch (Exception e)
            {
                var message = $"Could not find module with type '{jObject.GetValue("Type").Value<string>()}', please add it or remove manually from configuration file.";
                Logger.Log(Logger.Level.Error, message);
                throw new Exception(message);
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(IModule).IsAssignableFrom(objectType);
        }
    }
}
