using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Core;
using DAT10.Metadata.Model;
using DAT10.Modules.Inference;

namespace DAT10.Modules.Refinement.ColumnName
{
    public class PatternMatchName : RefinementModuleBase
    {
        private readonly DataSampleService _sampleService;
        private PatternMatchNamesConfiguration _configuration;
        public override string Name { get; } = "Column Name Pattern";
        public override string Description { get; } = "Deduce name of a column, by looking for predefined patterns in data samples.";


        public PatternMatchName(DataSampleService sampleService, SettingsService settingsService) : base(CommonDependency.None, CommonDependency.Name)
        {
            _sampleService = sampleService;
            _configuration = settingsService.LoadSetting<PatternMatchNamesConfiguration>(this, "Settings").Result;
        }

        public override async Task<CommonModel> Refine(CommonModel commonModel)
        {
            foreach (var table in commonModel.Tables)
            {
                foreach (var column in table.Columns)
                {
                    if(column.DataType.IsNumeric())
                        continue;

                    var result = await GuessName(column);

                    // If any confidence is set
                    if(result.Item2 > 0.01f)
                        column.AddNameCandidate(result.Item1, Math.Min(result.Item2, 0.5f));
                }
            }

            return commonModel;
        }

        private async Task<Tuple<string, float>> GuessName(Column column)
        {
            var datasample = await _sampleService.GetDataSampleAsync(column, 20);

            Dictionary<string, int> hits = new Dictionary<string, int>();

            // Iterate through patterns
            foreach (var configurationPattern in _configuration.Patterns)
            {
                // Iterate through data samples
                foreach (var sample in datasample)
                {
                    // If not match, then skip
                    if (!configurationPattern.Pattern.IsMatch(sample.Trim()))
                        continue;

                    // If any hit, then increment that value
                    if (!hits.ContainsKey(configurationPattern.PatternName))
                        hits[configurationPattern.PatternName] = 0;

                    hits[configurationPattern.PatternName]++;
                }
            }

            // If no hits where found
            if(hits.Count == 0)
                return new Tuple<string, float>(string.Empty, 0f);

            // Get name with most hits
            var highestValue = hits.Aggregate((l, r) => l.Value > r.Value ? l : r);

            // If no more than 3 hits, then skip
            if(highestValue.Value <= 3)
                return new Tuple<string, float>(string.Empty, 0f);

            // Return the key with the highest value
            return new Tuple<string, float>(highestValue.Key, highestValue.Value / datasample.Count);
        }
    }
}
