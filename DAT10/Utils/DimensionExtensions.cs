using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Metadata.Model;
using DAT10.StarModelComponents;

namespace DAT10.Utils
{
    public static class DimensionExtensions
    {
        public static List<ColumnOrigin> GetOriginalTables(this StarModelTableBase dimension)
        {
            var columnOrigins = dimension.Columns.Where(c => !c.ColumnType.HasFlag(StarColumnType.SurrogateKey) && c.ColumnRef != null).GroupBy(c => c.ColumnRef.Table).Select(columns => new ColumnOrigin(columns.Key, new List<StarColumn>(columns))).ToList();

            return columnOrigins;
        }


        public static void RenameDuplicateColumns(this StarModelTableBase table)
        {
            var duplicateGroups = table.Columns.GroupBy(sc => sc.Name)
                .Where(group => group.Count() > 1);

            foreach (var groups in duplicateGroups)
            {
                foreach (var starColumn in groups)
                {
                    starColumn.Name = $"{starColumn.ColumnRef.Table.Name}_{starColumn.Name}";
                }
            }
        }

        public class ColumnOrigin
        {
            public Table Table;
            public List<StarColumn> Columns;

            public ColumnOrigin(Table table, List<StarColumn> columns)
            {
                Table = table;
                Columns = columns;
            }

            public string ColumnsString()
            {
                return string.Join(", ", Columns.Select(c => $" {c.ColumnRef.GetNameWithAlias()} AS [{c.Name}]"));
            }
        }
    }
}
