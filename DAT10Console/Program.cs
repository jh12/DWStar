using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DAT10.Core;
using DAT10.Metadata.Model;
using DAT10.Modules.Inference;
using DAT10.StarModelComponents;
using SimpleInjector;
using SimpleLogger;
using SimpleLogger.Logging;
using SimpleLogger.Logging.Formatters;

namespace DAT10Console
{
    class Program
    {
        private static readonly Container Container;

        static Program()
        {
            Logger.LoggerHandlerManager.AddHandler(new ColoredConsoleLoggerHandler(new ConsoleLogFormatter()));

            // 1. Create container
            Container = new Container();

            // 2. Add containers
            Container.Register<DataSampleService>(Lifestyle.Singleton);
            Container.Register<SettingsService>(Lifestyle.Singleton);

            // 3. Verify configuration
            Container.Verify();
        }

        static void Main(string[] args)
        {
            //Miminum threshold to continue on with 
            double STAR_FACT_THRESHOLD = 0.8;

            Stopwatch timer = Stopwatch.StartNew();
            Stopwatch phaseTimer = Stopwatch.StartNew();

            Logger.Log("Creating module engine...");
            var moduleEngine = ModuleEngine.CreateInstance(Container, Container.GetInstance<SettingsService>()).Result;

            // ==== Metadata phase ==== //

            Logger.Log("Metadata schemas");
            // Get databases
            var commonModels = moduleEngine.GetDatabases();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Finished metadata phase in {phaseTimer.Elapsed}");
            phaseTimer.Restart();
            Console.ResetColor();

            // ==== Refinement phase ==== //

            Logger.Log("Refinement phase");
            // Refine databases
            var refinedModels = moduleEngine.RefineDatabases(new List<CommonModel> {commonModels}).Result;

            foreach (var commonModelsTable in refinedModels.First().Tables)
            {
                foreach (var column in commonModelsTable.Columns)
                {
                    if(column.IsPrimaryKey())
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    var dt = column.DataType;
                    //Console.WriteLine($"\t{column.Name.PadRight(25)}{dt.Type.ToString().PadRight(12)} ({dt.Length})({dt.Precision})({dt.Precision})");
                    Console.WriteLine($"{commonModelsTable.Name}#{column.Name}#{dt.Type.ToString()}#{column.KeyString}#{column.ConstraintString}");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }

            Console.WriteLine("===============");
            foreach (var commonModelsTable in refinedModels.First().Tables)
            {
                IEnumerable<Relation> relations = commonModelsTable.Relations.Where(r => r.LinkTable == commonModelsTable);

                foreach (Relation relation in relations)
                {
                    Console.WriteLine(relation.ToString());
                }

            }

            Console.ResetColor();
            Console.WriteLine("Done");
            Console.ReadLine();

            // Group common models depending on relations
            var groupedCommonModels = moduleEngine.GroupCommonModels(refinedModels);
            Logger.Log($"Grouped common models and found {groupedCommonModels.Count} common models");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Finished refinement phase in {phaseTimer.Elapsed}");
            phaseTimer.Restart();
            Console.ResetColor();

            // ==== Star phase ==== //

            // Combine Tables
            List<CommonModel> combined = new List<CommonModel>();
            foreach (var cm in groupedCommonModels)
            {
                combined.AddRange(moduleEngine.GenerateCombinedCommonModels(cm));
            }
            Logger.Log(Logger.Level.Debug, $"Generated: {combined.Count} common model(s)");
            Logger.Log("Generating Fact tables");
            
            // Generate fact tables
            List<StarModel> starModels = new List<StarModel>();
            foreach (var groupedModel in combined)
            {
                starModels.AddRange(moduleEngine.GenerateStarModels(groupedModel, STAR_FACT_THRESHOLD));
            }

            Logger.Log($"Generated: {starModels.Count} fact table(s)");
            Logger.Log("Generating Star models");

            var newStarModels = moduleEngine.GenerateStarModelsAfterFact(starModels);

            Logger.Log($"Generated: {newStarModels.Count} star model(s)");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Finished star phase in {phaseTimer.Elapsed}");
            phaseTimer.Restart();
            Console.ResetColor();

            // ==== Star refinement phase ==== //

            Logger.Log("Refining Star models");
            var refinedStarModels = moduleEngine.RefineStarModels(newStarModels);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Finished star refinement phase in {phaseTimer.Elapsed}");
            phaseTimer.Restart();
            Console.ResetColor();

            // ==== Generation phase ==== //

            Logger.Log("Generation physical model for star models");
            moduleEngine.GenerateModels(refinedStarModels).Wait();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Finished generation phase in {phaseTimer.Elapsed}");
            phaseTimer.Restart();
            Console.ResetColor();

            timer.Stop();
            Console.WriteLine(timer.Elapsed);

            Console.WriteLine("Finished");
            Console.ReadLine();
        }

        public class ConsoleLogFormatter : ILoggerFormatter
        {
            public string ApplyFormat(LogMessage logMessage)
            {
                return $"{logMessage.DateTime:HH:mm:ss}: [{logMessage.CallingMethod}:{logMessage.LineNumber}] {logMessage.Level}: {logMessage.Text}";
            }
        }

        public class ColoredConsoleLoggerHandler : ILoggerHandler
        {
            private readonly ILoggerFormatter _loggerFormatter;

            public ColoredConsoleLoggerHandler()
            {
                
            }

            public ColoredConsoleLoggerHandler(ILoggerFormatter loggerFormatter)
            {
                _loggerFormatter = loggerFormatter;
            }

            public void Publish(LogMessage logMessage)
            {
                var formattedMessage = _loggerFormatter.ApplyFormat(logMessage);

                switch (logMessage.Level)
                {
                    case Logger.Level.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case Logger.Level.Warning:
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        break;
                    default:
                        break;
                }

                Console.WriteLine(formattedMessage);
                Console.ResetColor();
            }
        }
    }
}
