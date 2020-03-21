using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Threading.Tasks;
using DAT10.Metadata.Model;
using DAT10.Modules.Inference;

namespace DAT10.Modules.Refinement.Datatypes
{
    public class ZipCodeIdentification : RefinementModuleBase
    {
        private readonly DataSampleService _sampleService;
        public override string Name { get; } = "Zip Code Identification";
        public override string Description { get; } = "Correct the data type of zip codes.";

        public ZipCodeIdentification(DataSampleService sampleService) : base(CommonDependency.DataType, CommonDependency.DataType)
        {
            _sampleService = sampleService;
        }

        public override async Task<CommonModel> Refine(CommonModel commonModel)
        {
          foreach (var table in commonModel.Tables)
          {
            foreach (var column in table.Columns)
            {
              DataType bestCandidate = column.DataType;

              if(bestCandidate.Type != OleDbType.Integer)
                  continue;

              List<string> values = await _sampleService.GetDataSampleAsync(column, 50);

              int leadingZeros = values.Count(v => v.StartsWith("0") && v.Length >= 4);

              if (leadingZeros > 0)
                  column.AddDatatypeCandidate(new DataType(OleDbType.VarWChar), 0.8f);
            }
          }

          return commonModel;
        }
    }
}