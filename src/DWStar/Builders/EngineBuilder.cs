using System.Collections.Generic;
using System.Linq;
using DWStar.Builders.Exceptions;
using DWStar.Builders.Interfaces;
using DWStar.DataAccess.Models;
using DWStar.DataAccess.SqlServer.Models;
using DWStar.Engine;
using DWStar.Engine.Interfaces;
using DWStar.Modules.Common;
using DWStar.Modules.Common.Generation.Interfaces;
using DWStar.Modules.Common.Metadata.Interfaces;
using DWStar.Modules.Common.Refinement.Interfaces;
using DWStar.Modules.Common.Star.Interfaces;
using DWStar.Modules.Common.StarRefinement.Interfaces;
using DWStar.Shared.Extensions;

namespace DWStar.Builders
{
    public class EngineBuilder : IEngineBuilder
    {
        private List<ConnectionInfo> _connectionInfos;

        private List<IMetadataModule> _metadataModules;
        private List<IRefinementModule> _refinementModules;
        private List<IStarModule> _starModules;
        private List<IStarRefinementModule> _starRefinementModules;
        private List<IGenerationModule> _generationModules;

        protected EngineBuilder()
        {
            _connectionInfos = new List<ConnectionInfo>();
            _metadataModules = new List<IMetadataModule>();
            _refinementModules = new List<IRefinementModule>();
            _starModules = new List<IStarModule>();
            _starRefinementModules = new List<IStarRefinementModule>();
            _generationModules = new List<IGenerationModule>();
        }

        public static IEngineBuilder CreateBuilder()
        {
            return new EngineBuilder();
        }

        public IEngineBuilder WithSqlServerConnection(SqlServerConnectionString connectionString)
        {
            _connectionInfos.Add(connectionString);

            return this;
        }

        public IEngineBuilder WithMetadataModule(IMetadataModule metadataModule)
        {
            _metadataModules.Add(metadataModule);

            return this;
        }

        public IEngineBuilder WithMetadataModules(IEnumerable<IMetadataModule> metadataModules)
        {
            _metadataModules.AddRange(metadataModules);

            return this;
        }

        public IEngineBuilder WithRefinementModule(IRefinementModule refinementModule)
        {
            _refinementModules.Add(refinementModule);

            return this;
        }

        public IEngineBuilder WithRefinementModules(IEnumerable<IRefinementModule> refinementModules)
        {
            _refinementModules.AddRange(refinementModules);

            return this;
        }

        public IEngineBuilder WithStarModule(IStarModule starModule)
        {
            _starModules.Add(starModule);

            return this;
        }

        public IEngineBuilder WithStarModules(IEnumerable<IStarModule> starModules)
        {
            _starModules.AddRange(starModules);

            return this;
        }

        public IEngineBuilder WithStarRefinementModule(IStarRefinementModule starRefinementModule)
        {
            _starRefinementModules.Add(starRefinementModule);

            return this;
        }

        public IEngineBuilder WithStarRefinementModules(IEnumerable<IStarRefinementModule> starRefinementModules)
        {
            _starRefinementModules.AddRange(starRefinementModules);

            return this;
        }

        public IEngineBuilder WithGenerationModule(IGenerationModule generationModule)
        {
            _generationModules.Add(generationModule);

            return this;
        }

        public IEngineBuilder WithGenerationModules(IEnumerable<IGenerationModule> generationModules)
        {
            _generationModules.AddRange(generationModules);

            return this;
        }

        public IStarEngine Build()
        {
            if(!_connectionInfos.Any())
                throw new BuildValidationException("No connections defined");

            if(!_metadataModules.Any())
                throw new BuildValidationException("No metadata modules defined");

            if(!_refinementModules.Any())
                throw new BuildValidationException("No refinement modules defined");

            if(!_starModules.Any())
                throw new BuildValidationException("No star modules defined");

            if(!_starRefinementModules.Any())
                throw new BuildValidationException("No star refinement modules defined");

            if(!_generationModules.Any())
                throw new BuildValidationException("No generation modules defined");

            return new StarEngine(_connectionInfos, _metadataModules, OrderDependencies(_refinementModules), _starModules, OrderDependencies(_starRefinementModules), _generationModules);
        }

        private IEnumerable<T> OrderDependencies<T>(IEnumerable<T> modules) where T : IModule
        {
            IEnumerable<T> noDepends = modules.NotOfType<T, IDepends>();

            IEnumerable<IDepends> depends = modules.OfType<IDepends>();

            // TODO: Implement ordering algorithm

            return noDepends.Concat(depends.Cast<T>());
        }
    }
}