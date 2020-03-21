using System;
using System.Collections.Generic;
using System.Linq;
using DAT10.Modules;
using SimpleInjector;

namespace DAT10.Core
{
    /// <summary>
    /// Load modules
    /// </summary>
    public class ModuleLoader
    {
        private readonly Container _container;

        public ModuleLoader(Container container)
        {
            _container = container;
        }

        /// <summary>
        /// Get the list of modules supplied with the program
        /// </summary>
        /// <returns>List of module types</returns>
        public List<Type> GetSuppliedModules()
        {
            var type = typeof(IModule);
            // Get the list of types that is assignable from IModule, is a class (but not abstract)
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract && p.Name != "ModuleStatus");

            return types.ToList();
        }

        // TODO: Not implemented
        /// <summary>
        /// Get a list of modules that the user has installed
        /// </summary>
        /// <returns>List of module types</returns>
        public List<Type> GetInstalledModules()
        {
            return new List<Type>();
        }

        /// <summary>
        /// Create instances for a list of module types
        /// </summary>
        /// <param name="moduleTypes">List of types</param>
        /// <returns>List of modules</returns>
        public List<IModule> CreateInstances(List<Type> moduleTypes)
        {
            // Create modules using an IOC container
            return moduleTypes.Where(t => t != null).Select(t => (IModule)_container.GetInstance(t)).ToList();
        }
    }
}
