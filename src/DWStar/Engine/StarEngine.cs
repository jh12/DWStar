using System.Collections.Generic;
using System.Threading.Tasks;
using DWStar.DataAccess.Models;
using DWStar.Engine.Interfaces;
using DWStar.Modules.Common.Generation.Interfaces;
using DWStar.Modules.Common.Metadata.Interfaces;
using DWStar.Modules.Common.Refinement.Interfaces;
using DWStar.Modules.Common.Star.Interfaces;
using DWStar.Modules.Common.StarRefinement.Interfaces;

namespace DWStar.Engine
{
    public class StarEngine : IStarEngine
    {
        private readonly IEnumerable<ConnectionInfo> _connectionInfos;
        private readonly IEnumerable<IMetadataModule> _metadataModules;
        private readonly IEnumerable<IRefinementModule> _refinementModules;
        private readonly IEnumerable<IStarModule> _starModules;
        private readonly IEnumerable<IStarRefinementModule> _starRefinementModules;
        private readonly IEnumerable<IGenerationModule> _generationModules;

        internal StarEngine(IEnumerable<ConnectionInfo> connectionInfos, IEnumerable<IMetadataModule> metadataModules, IEnumerable<IRefinementModule> refinementModules, IEnumerable<IStarModule> starModules, IEnumerable<IStarRefinementModule> starRefinementModules, IEnumerable<IGenerationModule> generationModules)
        {
            _connectionInfos = connectionInfos;
            _metadataModules = metadataModules;
            _refinementModules = refinementModules;
            _starModules = starModules;
            _starRefinementModules = starRefinementModules;
            _generationModules = generationModules;
        }

        public async Task<object> GetMetadata(IEnumerable<ConnectionInfo> connectionInfos)
        {
            throw new System.NotImplementedException();
        }
    }
}