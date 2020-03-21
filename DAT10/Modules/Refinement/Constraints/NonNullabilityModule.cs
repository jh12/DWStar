using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Metadata.Model;
using DAT10.Modules.Inference;

namespace DAT10.Modules.Refinement.Constraints
{
    public class NonNullabilityModule : RefinementModuleBase
    {
        private readonly DataSampleService _sampleService;
        public override string Name { get; } = "Find Not Nullable";
        public override string Description { get; } = "Deduces the nullability of a column by checking for null values in a data sample.";

        public NonNullabilityModule(DataSampleService sampleService) : base(CommonDependency.None, CommonDependency.NonNullable)
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
            // Check non-nullability of columns
            foreach (var column in table.Columns)
            {
                if (table.IsNotNullable(column)?.Confidence > 0.25f)
                    continue;

                await CheckColumn(column);
            }
        }

        private async Task CheckColumn(Column column)
        {
            var dataSample = await _sampleService.GetDataSampleAsync(column);

            // Check if any value is null
            foreach (var s in dataSample)
            {
                if (string.IsNullOrEmpty(s))
                    return;
            }

            // Add column as being not nullable
            column.Table.AddNotNullableCandidate(new NotNullable(column, 0.8f));
        }
    }
}
