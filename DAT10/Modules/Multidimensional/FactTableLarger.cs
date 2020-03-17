using System.Collections.Generic;
using System.Linq;
using DAT10.Metadata.Model;
using DAT10.StarModelComponents;
using SimpleLogger;

namespace DAT10.Modules.Multidimensional
{
    // TODO: How about column count?
    public class FactTableLarger : IMultidimModuleFact
    {
        public string Name { get; } = "Fact More Rows";
        public string Description { get; } = "Applies confidence on the likelihood of a table being a fact table based on the assumption that a table with a large amount of rows compared to other tables is more likely to be a fact table.";

        public List<StarModel> TranslateModel(CommonModel commonModel)
        {
            List<Table> tables = commonModel.Tables;

            List<StarModel> factConfidence = new List<StarModel>();

            if (tables.Count(x => x.RowCount != 0) == 0)
            {
                Logger.Log(Logger.Level.Debug, $"Heuristic '{Name}' skipped since no data was found in any table.");
                return factConfidence;
            }

            //Find average table row count across all tables of all databases
            double avgRowCount = tables.Where(t => t.RowCount != 0).Average(x => x.RowCount);

            if (tables.Count(x => x.RowCount == -1) != 0)
                return factConfidence;

            foreach (Table table in tables)
            {
                //TODO: andet måde?
                float confidence = 0.0f;
                //Find how many times larger than the average
                if (table.RowCount * 10 > avgRowCount)
                {
                    confidence = 1.0f;
                } else if (table.RowCount * 5 > avgRowCount)
                {
                    confidence = 0.5f;
                }
                else if (table.RowCount * 2 > avgRowCount)
                {
                    confidence = 0.2f;
                }

                //Convert table -> fact table
                FactTable factTable = new FactTable(table, confidence);

                factConfidence.Add(new StarModel(factTable, commonModel));
            }

            return factConfidence;
        }
    }
}
