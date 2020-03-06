using System.Collections.Generic;
using DWStar.DataAccess.SqlServer.Models;
using DWStar.Engine.Interfaces;
using DWStar.Modules.Common.Generation.Interfaces;
using DWStar.Modules.Common.Metadata.Interfaces;
using DWStar.Modules.Common.Refinement.Interfaces;
using DWStar.Modules.Common.Star.Interfaces;
using DWStar.Modules.Common.StarRefinement.Interfaces;

namespace DWStar.Builders.Interfaces
{
    public interface IEngineBuilder
    {
        IEngineBuilder WithSqlServerConnection(SqlServerConnectionString connectionString);

        IEngineBuilder WithMetadataModule(IMetadataModule metadataModule);
        IEngineBuilder WithMetadataModules(IEnumerable<IMetadataModule> metadataModules);

        IEngineBuilder WithRefinementModule(IRefinementModule refinementModule);
        IEngineBuilder WithRefinementModules(IEnumerable<IRefinementModule> refinementModules);

        IEngineBuilder WithStarModule(IStarModule starModule);
        IEngineBuilder WithStarModules(IEnumerable<IStarModule> starModules);

        IEngineBuilder WithStarRefinementModule(IStarRefinementModule starRefinementModule);
        IEngineBuilder WithStarRefinementModules(IEnumerable<IStarRefinementModule> starRefinementModules);

        IEngineBuilder WithGenerationModule(IGenerationModule generationModule);
        IEngineBuilder WithGenerationModules(IEnumerable<IGenerationModule> generationModules);

        IStarEngine Build();
    }
}