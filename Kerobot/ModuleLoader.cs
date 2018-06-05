using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Kerobot
{
    static class ModuleLoader
    {
        private const string LogName = nameof(ModuleLoader);

        /// <summary>
        /// Given the instance configuration, loads all appropriate types from file specified in it.
        /// </summary>
        internal static ReadOnlyCollection<ModuleBase> Load(InstanceConfig conf, Kerobot k)
        {
            var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar;
            var modules = new List<ModuleBase>();

            try
            {
                foreach (var file in conf.EnabledAssemblies)
                {
                    var a = Assembly.LoadFile(path + file);
                    modules.AddRange(LoadModulesFromAssembly(a, k));
                }
                return modules.AsReadOnly();
            }
            catch (Exception ex)
            {
                // TODO better (not lazy) exception handling
                // Possible exceptions:
                // - Errors loading assemblies
                // - Errors finding module paths
                // - Errors creating module instances
                // - Unknown errors
                Console.WriteLine("Module load failed.");
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        static IEnumerable<ModuleBase> LoadModulesFromAssembly(Assembly asm, Kerobot k)
        {
            var eligibleTypes = from type in asm.GetTypes()
                                where !type.IsAssignableFrom(typeof(ModuleBase))
                                where type.GetCustomAttribute<KerobotModuleAttribute>() != null
                                select type;
            k.InstanceLogAsync(false, LogName,
                $"{asm.FullName} has {eligibleTypes.Count()} usable types:");

            var newmods = new List<ModuleBase>();
            foreach (var t in eligibleTypes)
            {
                var mod = Activator.CreateInstance(t, k);
                k.InstanceLogAsync(false, LogName,
                    $"---> Instance created: {t.FullName}");
                newmods.Add((ModuleBase)mod);
            }
            return newmods;
        }
    }
}
