using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Metadata.Model;
using DAT10.Modules.Generation;
using DAT10.StarModelComponents;

namespace DAT10.Utils
{
    public static class TableExtensions
    {
        /// <summary>
        /// Add relation to table. Also adds it to the other involved table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="relation">Relation to add</param>
        public static void AddRelation(this Table table, Relation relation)
        {
            relation.AnchorTable.Relations.Add(relation);
            relation.LinkTable.Relations.Add(relation);
        }

        /// <summary>
        /// Check if any relation exists between the to tables. Columns are ignored in this check
        /// </summary>
        /// <param name="table">First table</param>
        /// <param name="otherTable">Second table</param>
        /// <returns>True if any relation exists between the tables</returns>
        public static bool HasRelationToTable(this Table table, Table otherTable)
        {
            bool linkRelation = table.Relations.Any(r => r.AnchorTable == table && r.LinkTable == otherTable);
            bool anchorRelation = table.Relations.Any(r => r.LinkTable == table && r.AnchorTable == otherTable);

            return linkRelation || anchorRelation;
        }

        public static string GetFullName(this Table table, bool escaped = true)
        {
            if (escaped)
                return $"[{table.Schema ?? "dbo"}].[{table.Name}]";

            return $"{table.Schema ?? "dbo"}.{table.Name}";
        }

        public static string GetFullNameWithDatabase(this Table table, bool escaped = true)
        {
            if (escaped)
                return $"[{table.Database.Name}].[{table.Schema ?? "dbo"}].[{table.Name}]";

            return $"{table.Database.Name}.{table.Schema ?? "dbo"}.{table.Name}";
        }

        public static string GetFullName(this StarModelTableBase table, bool escaped = true)
        {
            if (escaped)
                return $"[dbo].[{table.Name}]";

            return $"dbo.{table.Name}";
        }

        public static string GetNameAsAlias(this Table table, bool withDatabase = true)
        {
            return $"{(withDatabase ? table.GetFullNameWithDatabase() : table.GetFullName())} AS [{AliasService.Instance.GetAlias(table)}]";
        }

        public static string GetAlias(this Table table)
        {
            return AliasService.Instance.GetAlias(table);
        }

        public static string GetNameAsAlias(this StarModelTableBase table)
        {
            return $"{table.GetFullName()} AS [{AliasService.Instance.GetAlias(table)}]";
        }

        public static string GetAlias(this StarModelTableBase table)
        {
            return AliasService.Instance.GetAlias(table);
        }
    }
}
