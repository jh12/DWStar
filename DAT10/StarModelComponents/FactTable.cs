using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DAT10.Metadata.Model;

namespace DAT10.StarModelComponents
{
    [DebuggerDisplay("{Name} ({Confidence * 100}%)")]
    public class FactTable : StarModelTableBase
    {
        /// <summary>
        /// Confidence
        /// </summary>
        public float Confidence { get; set; }

        public FactTable(Table tableRef, float confidence) : base(tableRef)
        {
            Confidence = confidence;
        }

        public FactTable(string name, List<StarColumn> columns, Table tableReference, float confidence) : base(name, columns, tableReference)
        {
            Confidence = confidence;
        }


        #region Equality
        //Only "TableRef" is considered of the fields for equality
        protected bool Equals(FactTable other)
        {
            return TableReference.Equals(other.TableReference);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FactTable)obj);
        }

        public override int GetHashCode()
        {
            return (TableReference != null ? TableReference.GetHashCode() : 0);
        }
        #endregion
    }


}
