using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DAT10.Core.GenericGraph;
using DAT10.Core.Setting;
using DAT10.Metadata.Model;
using DAT10.Modules;
using DAT10.Modules.CombinationPhase;
using DAT10.Modules.Dimensions;
using DAT10.Modules.Generation;
using DAT10.Modules.Inference;
using DAT10.Modules.Multidimensional;
using DAT10.Modules.Refinement;
using DAT10.Modules.StarRefinement;
using DAT10.StarModelComponents;
using DAT10.Utils;
using Newtonsoft.Json;
using QuickGraph.Algorithms.Search;
using SimpleInjector;
using SimpleLogger;

namespace DAT10.Core
{
    public class ModuleEngine
    {
        #region Properties

        private Container _container;
        private readonly SettingsService _settingsService;
        private CoreSettings _coreSettings;

        // Modules
        private List<IModule> _modules = new List<IModule>();

        public List<InferenceModuleBase> SchemaInferenceModules { get; private set; }
        public List<RefinementModuleBase> RefinementModules { get; private set; }

        public List<ICombinationModule> CombinationModule { get; private set; }
        public List<IMultidimModuleFact> MultidimModules { get; private set; }
        public List<DimensionalModuleBase> DimensionalModules { get; private set; }

        public List<IStarRefinement> StarRefinementModules { get; private set; }
        public List<IGeneration> GenerationModules { get; private set; }

        // Configurations
        public ConfigurationManager ConfigurationManager { get; private set; }
        public Configuration CurrentConfiguration { get; set; }
        public List<Configuration> Configurations { get; private set; }

        // Connections
        public List<ConnectionInfo> Connections
        {
            get { return _coreSettings.Connections; }
            set { _coreSettings.Connections = value; }
        }

        public static string ProgramPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        public static string ModulePath(IModule module) => Path.Combine(ProgramPath, "Modules", module.Name.ToPascalCase());
        public static string ResultPath => Path.Combine(ProgramPath, "Results");

        #endregion

        #region Initialization

        protected ModuleEngine(Container container, SettingsService settingsService)
        {
            _container = container;
            _settingsService = settingsService;

            if (!Directory.Exists(ProgramPath))
                Directory.CreateDirectory(ProgramPath);

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
            };
        }

        /// <summary>
        /// Create an instance of the Module Engine
        /// </summary>
        /// <param name="container">IOC container</param>
        /// <returns>An instance of a module engine</returns>
        public static async Task<ModuleEngine> CreateInstance(Container container, SettingsService settingsService)
        {
            // Create instance
            var @this = new ModuleEngine(container, settingsService);

            @this._coreSettings = await settingsService.LoadSetting<CoreSettings>(@this, "Settings");

            // Find and instantiate modules
            List<Type> moduleTypes = new List<Type>();

            var moduleLoader = new ModuleLoader(container);
            moduleTypes.AddRange(moduleLoader.GetSuppliedModules());
            moduleTypes.AddRange(moduleLoader.GetInstalledModules());

            @this._modules = moduleLoader.CreateInstances(moduleTypes);

            // Seperate and order modules
            @this.SchemaInferenceModules = @this._modules.Where(m => m is InferenceModuleBase).Cast<InferenceModuleBase>().ToList();
            @this.RefinementModules = OrderDependencies<RefinementModuleBase>(@this._modules.Where(m => m is RefinementModuleBase).Cast<IDependent>().ToList());
            @this.CombinationModule = @this._modules.Where(m => m is ICombinationModule).Cast<ICombinationModule>().ToList();
            @this.MultidimModules = @this._modules.Where(m => m is IMultidimModuleFact).Cast<IMultidimModuleFact>().ToList();
            @this.DimensionalModules = OrderDependencies<DimensionalModuleBase>(@this._modules.Where(m => m is DimensionalModuleBase).Cast<IDependent>().ToList());

            @this.StarRefinementModules = @this._modules.Where(m => m is IStarRefinement).Cast<IStarRefinement>().ToList();
            @this.GenerationModules = @this._modules.Where(m => m is IGeneration).Cast<IGeneration>().ToList();

            // Load configurations
            @this.ConfigurationManager = new ConfigurationManager(container, @this._modules);
            @this.Configurations = await @this.ConfigurationManager.LoadConfigurations();

            if (@this.Configurations.Count == 0)
                @this = @this.CreateDefaultConfiguration();

            @this.CurrentConfiguration = @this.Configurations.First();

            // Load connections
            if (@this.Connections.Count == 0)
                @this = @this.CreateDefaultConnections();

            return @this;
        }

        /// <summary>
        /// Create a default configuration for the module engine
        /// </summary>
        /// <returns>Module engine</returns>
        public ModuleEngine CreateDefaultConfiguration()
        {
            var configuration = new Configuration();
            configuration.Name = "Default";

            configuration.InferenceModules = SchemaInferenceModules;
            configuration.RefinementModules = RefinementModules;
            configuration.MultidimFactModules = MultidimModules;
            configuration.DimensionModules = DimensionalModules;
            configuration.CombinationModule = CombinationModule.Take(1).ToList();
            configuration.StarRefinementModules = StarRefinementModules;
            configuration.GenerationModules = GenerationModules;

            Configurations.Add(configuration);
            CurrentConfiguration = configuration;

            return this;
        }

        public ModuleEngine CreateDefaultConnections()
        {
            //Connections.Add(new ConnectionInfo(@"D:\Jacob\Documents\AAU\DAT 10\DPT104F17\Database\Test Databases\Northwind CSV files", "CSV"));
            Connections.Add(new ConnectionInfo("Server=localhost;Database=DAT10-2;Trusted_Connection=True;TimeOut=4;", "SQL Server"));
            //Connections.Add(new ConnectionInfo("Server=localhost;Database=FKlub;Trusted_Connection=True;TimeOut=4;", "SQL Server"));

            return this;
        }

        #endregion

        #region Phases

        /// <summary>
        /// INFERENCE PHASE: Get databases for stored connection infos
        /// </summary>
        /// <returns>A list of databases</returns>
        public CommonModel GetDatabases()
        {
            var commonModels = GetDatabases(Connections);
            List<Table> tables = commonModels.SelectMany(x => x.Tables).ToList();

            return new CommonModel(tables);
        }

        /// <summary>
        /// METADATA PHASE: Get databases for connection infos
        /// </summary>
        /// <param name="connectionInfos">A list of connection infos</param>
        /// <returns>A list of databases</returns>
        public List<CommonModel> GetDatabases(List<ConnectionInfo> connectionInfos)
        {
            Logger.Log($"Executing 'schema inference phase' using configuration named '{CurrentConfiguration.Name}'.");

            List<CommonModel> results = new List<CommonModel>();

            // Create dictionary associating a source type to a inference module
            Dictionary<string, InferenceModuleBase> typeToInferenceModule = new Dictionary<string, InferenceModuleBase>();
            foreach (var module in CurrentConfiguration.InferenceModules)
            {
                if (typeToInferenceModule.ContainsKey(module.SupportedSourceType()))
                    continue;

                typeToInferenceModule[module.SupportedSourceType()] = module;
            }

            //Iterate through all connections and get their databases
            foreach (var connectionInfo in connectionInfos)
            {
                if (!typeToInferenceModule.ContainsKey(connectionInfo.ConnectionType))
                {
                    Logger.Log(Logger.Level.Warning, $"No inference module for the database type '{connectionInfo.ConnectionType}' exists.");
                    continue;
                }

                var module = typeToInferenceModule[connectionInfo.ConnectionType];

                if (!module.IsValidConnection(connectionInfo.ConnectionString))
                {
                    Logger.Log(Logger.Level.Warning, $"The connection string '{connectionInfo.ConnectionString}' is invalid according to the '{module.Name}' module.");
                    continue;
                }

                Logger.Log(Logger.Level.Debug, $"Executing inference module '{module.Name}'.");

                CommonModel commonModel;
                //try
                //{
                commonModel = module.GetSchema(connectionInfo.ConnectionString);
                //}
                //catch (Exception e)
                //{
                //    Logger.Log(e);
                //    continue;
                //}

                results.Add(commonModel);
            }

            return results;
        }

        /// <summary>
        /// REFINEMENT PHASE: Refine the supplied databases using the given refinement configuration
        /// </summary>
        /// <param name="commonModels">List of databases to refine</param>
        /// <returns>A list of refined databases</returns>
        public async Task<List<CommonModel>> RefineDatabases(List<CommonModel> commonModels)
        {
            Logger.Log($"Refining {commonModels.Count} common models.");

            // Iterate through all active modules, order is preserved
            foreach (var module in CurrentConfiguration.RefinementModules)
            {
                try
                {
                    Logger.Log(Logger.Level.Debug, $"Executing refinement module '{module.Name}'.");

                    foreach (var commonModel in commonModels)
                    {
                        await module.Refine(commonModel);
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(Logger.Level.Error, $"Module '{module.Name}' failed execution, and is therefore skipped.");
                    Logger.Log(e);
                    continue;

                }
            }

            return commonModels;
        }

        public List<CommonModel> GenerateCombinedCommonModels(CommonModel cm)
        {
            if (CurrentConfiguration.CombinationModule.Count == 0)
            {
                Logger.Log(Logger.Level.Info, "No combination modules is present. Skipping COMBINATION subphase.");
                return new List<CommonModel> { cm };
            }

            // Combine Tables
            return CurrentConfiguration.CombinationModule[0].Combine(cm);
        }

        /// <summary>
        /// STAR PHASE (FACTS): Generate one or more star models using the star modules
        /// </summary>
        /// <param name="commonModel"> A common model to generate star models for</param>
        /// <param name="threshold"> Threshold of confidence for fact tables: Avg(conf.) >= threshold to be fact table.</param>
        /// <returns>A list of star models</returns>
        public List<StarModel> GenerateStarModels(CommonModel commonModel, double threshold)
        {
            //step 1: Run all modules and collect all fact tables + all individal confidences
            List<StarModel> collectedConfidences = new List<StarModel>();
            foreach (var module in CurrentConfiguration.MultidimFactModules)
            {
                collectedConfidences.AddRange(module.TranslateModel(commonModel));
            }

            //Get simple list of all fact tables + remove duplicates
            List<FactTable> factTables = collectedConfidences.Select(sm => sm.FactTable).Distinct().ToList();

            //Step 2: Get all confidences for each fact table and get average.
            List<StarModel> averageConfidence = new List<StarModel>();
            foreach (FactTable factTable in factTables)
            {
                int confidenceCount = 0;        //num of confidences for fact table
                float confidenceSum = 0.0f;     //confidences summed up

                //For each dictionary that we got from each module
                foreach (StarModel starModel in collectedConfidences)
                {
                    //Get the confidence if it exists.
                    if (factTable.Equals(starModel.FactTable))
                    {
                        confidenceSum += starModel.FactTable.Confidence;
                        confidenceCount++;
                    }
                }

                //Find average confidence and add to final dictionary - IF upholds threshold
                float finalConfidence = confidenceSum / confidenceCount;
                if (finalConfidence >= threshold && CheckCombinedTableCondition(commonModel, factTable.TableReference))
                {
                    factTable.Confidence = finalConfidence;
                    averageConfidence.Add(new StarModel(factTable, commonModel));
                }

            }

            return averageConfidence;
        }

        private bool CheckCombinedTableCondition(CommonModel commonModel, Table factTable)
        {
            if (commonModel.Tables.Count(x => x is CombinedTable) != 0)
                return factTable is CombinedTable;
            else
                return true;

        }

        /// <summary>
        /// STAR PHASE (rest): Populate starmodels with dimensions and other related values
        /// </summary>
        /// <param name="starModels">List of star models</param>
        /// <returns>List of star models</returns>
        public List<StarModel> GenerateStarModelsAfterFact(List<StarModel> starModels)
        {
            foreach (var module in CurrentConfiguration.DimensionModules)
            {
                for (int i = 0; i < starModels.Count; i++)
                {
                    var starModel = starModels[i];
                    starModels[i] = module.TranslateModel(starModel);
                }
            }

            return starModels.Where(s => s.Dimensions.Count > _coreSettings.StarPhaseSettings.FilterStarModelsWithLessThanDimensions).ToList();
        }

        /// <summary>
        /// STAR REFINEMENT PHASE: Refine the supplied star models
        /// </summary>
        /// <param name="starModels">Star models to refine</param>
        /// <returns>A list of refined star models</returns>
        public List<StarModel> RefineStarModels(List<StarModel> starModels)
        {
            foreach (var module in CurrentConfiguration.StarRefinementModules)
            {
                for (var i = 0; i < starModels.Count; i++)
                {
                    var starModel = starModels[i];
                    starModels[i] = module.Refine(starModel);
                }
            }

            return starModels;
        }

        /// <summary>
        /// GENERATION PHASE: Generate useful representations of star models
        /// </summary>
        /// <param name="starModels">List of star models to generate</param>
        public async Task GenerateModels(List<StarModel> starModels)
        {
            if (!Directory.Exists(ResultPath))
                Directory.CreateDirectory(ResultPath);

            // Change name of duplicate columns
            foreach (var starModel in starModels)
            {
                starModel.FactTable.RenameDuplicateColumns();
                foreach (var dimension in starModel.Dimensions)
                {
                    dimension.RenameDuplicateColumns();
                }
            }

            foreach (var module in CurrentConfiguration.GenerationModules)
            {
                foreach (var starModel in starModels)
                {
                    try
                    {
                        await module.Generate(starModel, ResultPath);
                    }
                    catch (Exception e)
                    {
                        Logger.Log(Logger.Level.Error, $"Generation module '{module.Name}' failed to execute on star model containing '{starModel.FactTable.Name}' as fact table.");
                        Logger.Log(e);
                        continue;
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Using relations split or merge tables together in common models
        /// </summary>
        /// <param name="commonModels">List of common models</param>
        /// <returns>Grouped list of common models</returns>
        public List<CommonModel> GroupCommonModels(List<CommonModel> commonModels)
        {
            List<CommonModel> groupedCommonModels = new List<CommonModel>();

            // Construct graph
            UnDirectedGraph<Table> graph = new UnDirectedGraph<Table>();
            commonModels.ForEach(c => c.AddRelationsToGraph(graph));

            // List of all not visited vertices
            HashSet<GenericNode<Table>> notVisited = new HashSet<GenericNode<Table>>(graph.Vertices);
            // List the current DFS has found
            List<Table> foundTables = new List<Table>();

            // Create an depthfirstsearch algorithm, which for each discovered vertex adds it to the found list
            var algorithm = new UndirectedDepthFirstSearchAlgorithm<GenericNode<Table>, GenericEdge<Table>>(graph);
            algorithm.DiscoverVertex += vertex =>
            {
                notVisited.Remove(vertex);
                foundTables.Add(vertex.Data);
            };

            // Perform depthfirstsearch on graph and group all related databases together in one common model
            do
            {
                // Get current root and run DFS
                var currRoot = notVisited.First();
                algorithm.Compute(currRoot);

                groupedCommonModels.Add(new CommonModel(foundTables));

                // Clean for next run
                foundTables.Clear();
            } while (notVisited.Count > 0);


            return groupedCommonModels;
        }

        /// <summary>
        /// Order a list of IDependent objects such that each dependency should be fulfilled
        /// granted that other modules is capable of doing that.
        /// </summary>
        /// <typeparam name="T">Type of result list</typeparam>
        /// <param name="modules">List of modules implementing IDependent</param>
        /// <returns>A sorted list of modules</returns>
        public static List<T> OrderDependencies<T>(List<IDependent> modules) where T : IModule
        {
            var graph = new DependencyGraph(modules);

            return new List<T>(graph.TopologicalSort().Cast<T>());
        }

        /// <summary>
        /// Save module engine
        /// </summary>
        /// <returns>Task</returns>
        public async Task Save()
        {
            await ConfigurationManager.SaveConfigurations(Configurations);
        }
    }
}
