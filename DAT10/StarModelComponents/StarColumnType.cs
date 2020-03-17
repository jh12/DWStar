using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT10.StarModelComponents
{
    [Flags]
    public enum StarColumnType : short
    {
        None = 0,
        Key = 1,
        SurrogateKey = 2,
        DescriptiveMeasure = 4,
        NumericMeasure = 8
    }
}
