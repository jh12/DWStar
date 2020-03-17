using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Metadata.Model;
using DAT10.StarModelComponents;

namespace DAT10.Utils
{
    public static class ColumnExtensions
    {
        public static string GetName(this Column column, bool escaped = true)
        {
            if (escaped)
                return $"[{column.Name}]";

            return $"{column.Name}";
        }

        public static string GetNameWithTable(this Column column, bool escaped = true)
        {
            if (escaped)
                return $"{column.Table.GetFullName()}.[{column.Name}]";

            return $"{column.Table.GetFullName(false)}.{column.Name}";
        }

        public static string GetNameWithTableAndDatabase(this Column column, bool escaped = true)
        {
            if (escaped)
                return $"[{column.Table.Database.Name}].{column.Table.GetFullName()}.[{column.Name}]";

            return $"{column.Table.Database.Name}.{column.Table.GetFullName(false)}.{column.Name}";
        }

        public static string GetName(this StarColumn column, bool escaped = true)
        {
            if (escaped)
                return $"[{TrimName(column.Name)}]";

            return $"{TrimName(column.Name)}";
        }

        public static string GetNameWithTable(this StarColumn column, bool escaped = true)
        {
            if (escaped)
                return $"{column.TableRef.GetFullName()}.[{TrimName(column.Name)}]";

            return $"{column.TableRef.GetFullName(false)}.{TrimName(column.Name)}";
        }

        public static string GetNameWithAlias(this Column column)
        {
            return $"[{column.Table.GetAlias()}].[{column.Name}]";
        }

        public static string GetNameAsAlias(this StarColumn column, StarModelTableBase dimension = null)
        {
            if(dimension == null)
                return $"[{column.TableRef.GetAlias()}].[{TrimName(column.Name)}] AS [{column.TableRef.GetAlias()}_{TrimName(column.Name)}]";

            return $"[{dimension.GetAlias()}].[{TrimName(column.Name)}] AS [{dimension.GetAlias()}_{TrimName(column.Name)}]";
        }

        public static string GetNameWithAlias(this StarColumn column, StarModelTableBase dimension = null)
        {
            if(dimension == null)
                return $"[{column.TableRef.GetAlias()}].[{TrimName(column.Name)}]";

            return $"[{dimension.GetAlias()}].[{TrimName(column.Name)}]";
        }

        public static string GetAliasName(this StarColumn column, StarModelTableBase dimension = null)
        {
            if(dimension == null)
                return $"[{column.TableRef.GetAlias()}_{TrimName(column.Name)}]";

            return $"[{dimension.GetAlias()}_{TrimName(column.Name)}]";
        }

        public static string TrimName(string name)
        {
            return name.Replace(" ", string.Empty).Replace("_", string.Empty);
        }
    }
}
