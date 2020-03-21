using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DAT10.Metadata.Model;
using DAT10.Modules.Inference;

namespace DAT10.Modules.Refinement.Constraints
{
    public class UniqueModule : RefinementModuleBase
    {
        private readonly DataSampleService _sampleService;
        public override string Name { get; } = "Find Unique Columns";
        public override string Description { get; } = "Deduces the uniqueness of a column by checking for repeating values in a data sample.";

        public UniqueModule(DataSampleService sampleService) : base(CommonDependency.NonNullable, CommonDependency.Unique)
        {
            _sampleService = sampleService;
        }

        public override async Task<CommonModel> Refine(CommonModel commonModel)
        {
            foreach (var table in commonModel.Tables)
            {
                await CheckTable(table);
            }

            return commonModel;
        }

        private async Task CheckTable(Table table)
        {
            foreach (var column in table.Columns)
            {
                if (table.IsUnique(column) != null)
                    continue;

                await CheckColumn(table, column);
            }


            // Check of combinations of columns.
            //for (int i = 0; i-1 < table.Columns.Count; i++)
            //{
            //    var column = table.Columns[i];
            //    var column2 = table.Columns[i+1];

            //    await CheckColumns(table, column, column2);
            //}
        }

        private async Task CheckColumn(Table table, Column column)
        {
            var data = await _sampleService.GetDataSampleAsync(column);

            if (data.Distinct().Count() != data.Count)
                return;

            Unique unique;
            unique = data.Count == 0 ? new Unique(new List<Column> {column}, 0.5f) : new Unique(new List<Column> {column}, 1 - 1 / data.Count); // TODO: Random 0.5f value

            table.AddUniqueCandidate(unique);
        }

        private async Task CheckColumns(Table table, params Column[] columns)
        {
            var data = await _sampleService.GetDataSampleAsync(columns);
        }
    }
}
