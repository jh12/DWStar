using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Metadata.Model;
using DAT10.Modules.Inference;

namespace DAT10.Modules.Refinement.Constraints
{
    public class DistinctPrimaryKeys : RefinementModuleBase
    {
        private readonly DataSampleService _sampleService;
        private new const CommonDependency Requires = CommonDependency.NonNullable | CommonDependency.Unique | CommonDependency.DataType | CommonDependency.PrimaryKey;

        public override string Name { get; } = "Find Primary Keys";
        public override string Description { get; } = "Attempt to find primary key candidates by looking at the nullability and uniqueness of columns.";

        public DistinctPrimaryKeys(DataSampleService sampleService) : base(Requires, CommonDependency.PrimaryKey)
        {
            _sampleService = sampleService;
        }

        public override async Task<CommonModel> Refine(CommonModel commonModel)
        {
            foreach (var table in commonModel.Tables)
            {
                // Check if table already has a primary key
                if (table.PrimaryKey?.Confidence > 0.25f)
                    continue;

                List<PrimaryKey> candidates = new List<PrimaryKey>();

                foreach (var column in table.Columns)
                {
                    var primaryKey = await CheckIfPrimaryKeyCandidate(table, column);
                    if(primaryKey.Confidence > 0.1f)
                        
                        candidates.Add(primaryKey);
                }

                candidates.ForEach(c => table.AddPrimaryCandidate(c));
            }

            return commonModel;
        }

        private async Task<PrimaryKey> CheckIfPrimaryKeyCandidate(Table table, Column column)
        {
            // Check if unique constraint already is defined on column
            if(table.IsUnique(column)?.Confidence > 0.5f)
                return new PrimaryKey(column, 0.8f);

            var list = await _sampleService.GetDataSampleAsync(column, 1000);

            if (list.Count == 0)
                return new PrimaryKey(column, 0f);

            var distinctValues = list.Distinct().Count();
            if (distinctValues == list.Count)
                return new PrimaryKey(column, 1 - 1 / list.Count);


            return new PrimaryKey(column, 0f);
        }
    }
}
