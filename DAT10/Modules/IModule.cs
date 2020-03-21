using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DAT10.Modules
{
    public interface IModule
    {
        [JsonIgnore]
        string Name { get; }
        [JsonIgnore]
        string Description { get; }
    }
}
