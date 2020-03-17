using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT10.Core.Setting
{
    public class CoreSettings
    {
        // Connections
        public List<ConnectionInfo> Connections;
        public StarPhase StarPhaseSettings;

        public class StarPhase
        {
            // Minimum amount of dimensions needed for a star model to exist
            public int FilterStarModelsWithLessThanDimensions = 0;
        }

        public CoreSettings()
        {
            Connections = new List<ConnectionInfo>();

            StarPhaseSettings = new StarPhase();
        }
    }
}
