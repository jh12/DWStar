using System;
using System.Collections.Generic;
using DAT10.StarModelComponents;

namespace DAT10.Modules.Dimensions
{
    public abstract class DimensionalModuleBase : IModule, IDependent
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        public abstract StarModel TranslateModel(StarModel starModel);

        protected DimensionalModuleBase(StarDependency requires, StarDependency affects)
        {
            Requires = (short) requires;
            Affects = (short) affects;
        }

        public short Requires { get; private set; }
        public short Affects { get; private set; }
        public bool IsDependentOn(IDependent module)
        {
            return (Requires & module.Affects) > 0;
        }

        public short AllDependencies { get; } = (short) StarDependency.ALL;

        public List<string> DependenciesToStrings(short dependenciesMask)
        {
            List<string> strings = new List<string>();

            dependenciesMask = (short)((short)~dependenciesMask & AllDependencies);
            var mask = (StarDependency)dependenciesMask;

            return new List<string>(mask.ToString().Split(new[] { ", " }, StringSplitOptions.None));
        }
    }
}
