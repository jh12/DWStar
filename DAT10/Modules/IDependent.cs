using System.Collections.Generic;
using Newtonsoft.Json;

namespace DAT10.Modules
{
    public interface IDependent
    {
        /// <summary>
        /// Which dependencies does the module require. Short represents enum flags
        /// </summary>
        [JsonIgnore]
        short Requires { get; }
        /// <summary>
        /// Which dependencies does the module affect. Short represents enum flags
        /// </summary>
        [JsonIgnore]
        short Affects { get; }

        bool IsDependentOn(IDependent module);

        /// <summary>
        /// The highest amount of dependencies that the module can affect
        /// </summary>
        [JsonIgnore]
        short AllDependencies { get; }

        /// <summary>
        /// Convert dependency enum flags into a human readable string
        /// </summary>
        List<string> DependenciesToStrings(short dependenciesMask);
    }
}
