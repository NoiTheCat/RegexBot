using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RegexBot
{
    static class ModuleLoader
    {
        private const string LogName = nameof(ModuleLoader);

        /// <summary>
        /// Given the instance configuration, loads all appropriate types from file specified in it.
        /// </summary>
        internal static ReadOnlyCollection<ModuleBase> Load(InstanceConfig conf, RegexbotClient k)
        {
            var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar;
            var modules = new List<ModuleBase>();

            foreach (var file in conf.EnabledAssemblies)
            {
                Assembly a = null;
                try
                {
                    a = Assembly.LoadFile(path + file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred when attempting to load a module assembly.");
                    Console.WriteLine($"File: {file}");
                    Console.WriteLine(ex.ToString());
                    Environment.Exit(2);
                }

                IEnumerable<ModuleBase> amods = null;
                try
                {
                    amods = LoadModulesFromAssembly(a, k);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred when attempting to create a module instance.");
                    Console.WriteLine(ex.ToString());
                    Environment.Exit(2);
                }
                modules.AddRange(amods);
            }
            return modules.AsReadOnly();
        }

        static IEnumerable<ModuleBase> LoadModulesFromAssembly(Assembly asm, RegexbotClient k)
        {
            var eligibleTypes = from type in asm.GetTypes()
                                where !type.IsAssignableFrom(typeof(ModuleBase))
                                where type.GetCustomAttribute<RegexbotModuleAttribute>() != null
                                select type;
            k.InstanceLogAsync(false, LogName, $"Scanning {asm.GetName().Name}");

            var newmods = new List<ModuleBase>();
            foreach (var t in eligibleTypes)
            {
                var mod = Activator.CreateInstance(t, k);
                k.InstanceLogAsync(false, LogName,
                    $"---> Loading module {t.FullName}");
                newmods.Add((ModuleBase)mod);
            }
            return newmods;
        }
    }
}
