using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Metadata.Model;

namespace DAT10.Modules.Refinement
{
    public abstract class RefinementModuleBase : IModule, IDependent
    {
        /// <summary>
        /// Retrieves a database object and performs various implementation specific refinements on it.
        /// </summary>
        /// <param name="database">Database instance to refine</param>
        /// <returns>Refined database instance. (Can be different from input object.)</returns>
        public abstract Task<CommonModel> Refine(CommonModel commonModel);


        public abstract string Name { get; }
        public abstract string Description { get; }

        public short Requires { get; }
        public short Affects { get; }

        protected RefinementModuleBase(CommonDependency requires, CommonDependency affects)
        {
            Requires = (short) requires;
            Affects = (short) affects;
        }

        public bool IsDependentOn(IDependent module)
        {
            return (Requires & module.Affects) > 0;
        }

        public short AllDependencies { get; } = (short) CommonDependency.ALL;

        public List<string> DependenciesToStrings(short dependenciesMask)
        {
            List<string> strings = new List<string>();

            dependenciesMask = (short) ((short) ~dependenciesMask & AllDependencies);
            var mask = (CommonDependency)dependenciesMask;

            return new List<string>(mask.ToString().Split(new [] {", "}, StringSplitOptions.None));
        }
    }
}
