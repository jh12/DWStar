using System;
using System.Collections.Generic;
using DAT10.Metadata.Model;

namespace DAT10.StarModelComponents.Dimensions
{
    public class DateDimension : Dimension
    {
        public new Table TableReference
        {
            get { throw new Exception(); }
        }

        public DateGranularity Granularity { get; set; }

        public DateDimension(string name, DateGranularity granularity) : base(name, new List<StarColumn>(), null)
        {
            Granularity = granularity;
        }

        public DateDimension(Dimension basedOn, bool isRolePlaying = true) : base(basedOn, isRolePlaying)
        {
        }

        public enum DateGranularity
        {
            Years,
            Quarters,
            Months,
            Weeks,
            Days
        }
    }
}
