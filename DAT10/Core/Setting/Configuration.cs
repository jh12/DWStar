using System.Collections.Generic;
using DAT10.Modules.CombinationPhase;
using DAT10.Modules.Dimensions;
using DAT10.Modules.Generation;
using DAT10.Modules.Inference;
using DAT10.Modules.Multidimensional;
using DAT10.Modules.Refinement;
using DAT10.Modules.StarRefinement;
using Newtonsoft.Json;

namespace DAT10.Core.Setting
{
    /// <summary>
    /// Defines a configuration, which specifies what modules to use in each phase
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Name of configuration
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Name that the configuration is currently saved under
        /// </summary>
        [JsonIgnore]
        public string SavedName { get; set; }

        public List<InferenceModuleBase> InferenceModules { get; set; }
        public List<RefinementModuleBase> RefinementModules { get; set; }

        public List<ICombinationModule> CombinationModule { get; set; }
        public List<IMultidimModuleFact> MultidimFactModules { get; set; }
        public List<DimensionalModuleBase> DimensionModules { get; set; }

        public List<IStarRefinement> StarRefinementModules { get; set; }
        public List<IGeneration> GenerationModules { get; set; }

        public Configuration()
        {
            InferenceModules = new List<InferenceModuleBase>();
            RefinementModules = new List<RefinementModuleBase>();
            CombinationModule = new List<ICombinationModule>();
            MultidimFactModules = new List<IMultidimModuleFact>();
            DimensionModules = new List<DimensionalModuleBase>();
            StarRefinementModules = new List<IStarRefinement>();
            GenerationModules = new List<IGeneration>();
        }
    }
}
