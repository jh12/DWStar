using System.Collections.Generic;
using System.Linq;
using DAT10.Metadata.Model;

namespace DAT10.StarModelComponents
{
    public abstract class StarModelTableBase
    {
        #region Properties

        private static int _nextIDValue;
        private static int NextID => _nextIDValue++;

        /// <summary>
        /// Unique ID
        /// Ensures that roleplaying tables are different from the original table
        /// </summary>
        private int ID;

        public bool IsRolePlaying { get; }
        public StarModelTableBase RoleplayBasedOn { get; }

        public StarModelTableBase ActualTable
        {
            get
            {
                if (IsRolePlaying)
                    return RoleplayBasedOn;

                return this;
            }
        }

        /// <summary>
        /// Name of table
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Columns in table
        /// </summary>
        public List<StarColumn> Columns { get; set; }

        /// <summary>
        /// Reference to associated table in common model
        /// </summary>
        public Table TableReference { get; }

        /// <summary>
        /// Relations to other Star model tables
        /// </summary>
        public List<StarRelation> Relations { get; }

        /// <summary>
        /// Constraints
        /// </summary>
        public StarConstraints Constraints { get; }

        #endregion

        protected StarModelTableBase(Table tableRef)
        {
            ID = NextID;
            Name = tableRef.Name;
            Columns = tableRef.Columns.Select(c => new StarColumn(c)).ToList();
            Columns.ForEach(c => c.TableRef = this);
            TableReference = tableRef;
            Relations = new List<StarRelation>();
            Constraints = new StarConstraints();
        }

        protected StarModelTableBase(string name, List<StarColumn> columns, Table tableReference)
        {
            ID = NextID;
            Name = name;
            Columns = columns;
            Columns.ForEach(c => c.TableRef = this);
            TableReference = tableReference;
            Relations = new List<StarRelation>();
            Constraints = new StarConstraints();
        }

        public StarModelTableBase(StarModelTableBase basedOn, bool isRolePlaying) : this(basedOn.Name, basedOn.Columns, basedOn.TableReference)
        {
            IsRolePlaying = isRolePlaying;
            RoleplayBasedOn = basedOn;
        }

        public bool HasKey() => Columns.Any(c => c.ColumnType.HasFlag(StarColumnType.Key));
    }
}
