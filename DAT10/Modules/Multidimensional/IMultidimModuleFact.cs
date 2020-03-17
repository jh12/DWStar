using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Metadata.Model;
using DAT10.StarModelComponents;

namespace DAT10.Modules.Multidimensional
{
    public interface IMultidimModuleFact : IModule
    {
        List<StarModel> TranslateModel(CommonModel commonModel);
    }
}
