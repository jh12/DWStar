using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Metadata.Model;

namespace DAT10.Modules.Refinement.Relations
{
    public class CardinalityModule : RefinementModuleBase
    {
        public CardinalityModule() : base(CommonDependency.Relations, CommonDependency.Cardinality)
        {

        }

        public override string Name { get; } = "Cardinality";
        public override string Description { get; } = "Deduces cardinality of table relationships based on data samples.";

        public override async Task<CommonModel> Refine(CommonModel commonModel)
        {

            foreach (var table in commonModel.Tables)
            {
                foreach (var relation in table.Relations)
                {
                    FindCardinality(relation);
                }
            }

            return commonModel;
        }

        private void FindCardinality(Relation relation)
        {
            // If cardinality is already set, then don't try to find a new one
            if (relation.Cardinality != Cardinality.Unknown)
                return;

            var foreignTable = relation.LinkTable;
            var foreignKey = relation.LinkColumns;

            // Check if any unique constraint exists on the foreign table
            var uniques = foreignTable.Uniques.FirstOrDefault(
                u => u.Columns.All(foreignKey.Contains) && u.Columns.Count == foreignKey.Count);

            // Check if any unique constraint exists on the foreign table
            var primaryKeys = foreignTable.PrimaryKeys.FirstOrDefault(
                pk => pk.Columns.All(foreignKey.Contains) && pk.Columns.Count == foreignKey.Count);

            // If column(s) participating in foreignkey is unique (or primarykey) then assume that cardinality is One-to-One
            if (uniques != null || primaryKeys != null)
            {
                relation.Cardinality = Cardinality.OneToOne;
                return;
            }

            relation.Cardinality = Cardinality.ManyToOne;
        }
    }
}
