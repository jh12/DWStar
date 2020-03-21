using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Metadata.Model;

namespace DAT10.Modules.CombinationPhase
{
    public interface ICombinationModule : IModule
    {
        List<CommonModel> Combine(CommonModel cm);
    }
}
