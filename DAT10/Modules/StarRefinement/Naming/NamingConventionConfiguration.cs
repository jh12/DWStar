using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DAT10.Modules.StarRefinement.Naming
{
    public class NamingConventionConfiguration
    {
        // Table level
        public string FactTableNameStructure = "Fact_%NAME%";
        public string DimensionNameStructure = "Dim_%NAME%";

        public string TableNameCasing = "PascalCase";
        public bool TableStripUnderscore = true;

        // Column level
        public string ColumnNameStructure = "%NAME%";
        public string ColumnNameCasing = "PascalCase";
        public bool ColumnStripUnderscore = true;
    }
}
