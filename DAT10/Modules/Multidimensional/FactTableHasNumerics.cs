using System.Collections.Generic;
using System.Linq;
using DAT10.Metadata.Model;
using DAT10.StarModelComponents;

namespace DAT10.Modules.Multidimensional
{
    class FactTableHasNumerics : IMultidimModuleFact
    {
        public string Name { get; } = "Fact More Columns";
        public string Description { get; } = "Applies confidence on the likelihood of a table being a fact table based on the amount of numeric columns.";

        public List<StarModel> TranslateModel(CommonModel commonModel)
        {
            List<Table> tables = commonModel.Tables;

            // TODO: Change calculation to avoid keys and to different ratio (if Søren decides if it is okay)
            //Find the ratio of numeric/not-numeric columns for each table 
            Dictionary<Table, double> ratios = new Dictionary<Table, double>();
            foreach (Table table in tables)
            {
                int numericColumns = table.Columns.Select(c => c.DataType.IsNumeric()).Count();
                //All other columns
                int nonNumericColumns = table.Columns.Count - numericColumns;
                double ratio;
                if (nonNumericColumns != 0)
                {
                    ratio = numericColumns / nonNumericColumns;
                }
                else
                {
                    //TODO: Rigtige måde at gøre dette på? 0 non-numeric -> bare sig antallet af ratio = antallet af numeric cols.
                    ratio = numericColumns;
                }

                ratios.Add(table, ratio);
            }

            //get confidence
            List<StarModel> factConfidence = new List<StarModel>();
            foreach (KeyValuePair<Table, double> pair in ratios)
            {
                //Find confidence?
                //TODO: better way to detemine confidence?
                float confidence = 0.0f;
                if (pair.Value >= 5)
                {
                    confidence = 1.0f;
                }
                else if (pair.Value >= 3)
                {
                    confidence = 0.5f;
                }
                else if (pair.Value >= 1)
                {
                    confidence = 0.2f;
                }

                //Convert table -> fact table
                FactTable factTable = new FactTable(pair.Key, confidence);

                //Add new star model
                factConfidence.Add(new StarModel(factTable, commonModel));
            }

            return factConfidence;



            //TODO: få confidence ind i det
            //TODO: better way to determine threshold?
            //If ratio >= 5 -> the table is a fact table
            //List<FactTable> chosenTables = ratios.Where(r => r.Value >= 5.0)
            //    .Select(dic => new FactTable() {
            //        Name = dic.Key.Name,
            //        TableRef = dic.Key,
            //        Columns = dic.Key.Columns.Select( col => new StarColumn(col))
            //        .ToList()})
            //    .ToList();

            //return chosenTables;
        }
    }
}
