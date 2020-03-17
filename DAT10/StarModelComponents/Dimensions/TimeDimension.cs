using System;
using System.Collections.Generic;
using DAT10.Metadata.Model;

namespace DAT10.StarModelComponents.Dimensions
{
    public class TimeDimension : Dimension
    {
        public new Table TableReference
        {
            get { throw new Exception();}
        }

        public TimeGranularity Granularity { get; set; }

        public TimeDimension(string name, TimeGranularity granularity) : base(name, new List<StarColumn>(), null)
        {
            Granularity = granularity;
        }

        public TimeDimension(Dimension basedOn, bool isRolePlaying = true) : base(basedOn, isRolePlaying)
        {
        }

        public enum TimeGranularity
        {
            Hours,
            Minutes,
            Seconds,
            Milliseconds
        }
    }
}
