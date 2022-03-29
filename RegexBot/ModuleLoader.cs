using System.Collections.ObjectModel;
using System.Reflection;

namespace RegexBot;

static class ModuleLoader {
    private const string LogName = nameof(ModuleLoader);

    /// <summary>
    /// Given the instance configuration, loads all appropriate types from file specified in it.
    /// </summary>
    internal static ReadOnlyCollection<RegexbotModule> Load(InstanceConfig conf, RegexbotClient k) {
        var path = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location) + Path.DirectorySeparatorChar;
        var modules = new List<RegexbotModule>();

        foreach (var file in conf.Assemblies) {
            Assembly? a = null;
            try {
                a = Assembly.LoadFile(path + file);
            } catch (Exception ex) {
                Console.WriteLine("An error occurred when attempting to load a module assembly.");
                Console.WriteLine($"File: {file}");
                Console.WriteLine(ex.ToString());
                Environment.Exit(2);
            }

            IEnumerable<RegexbotModule>? amods = null;
            try {
                amods = LoadModulesFromAssembly(a, k);
            } catch (Exception ex) {
                Console.WriteLine("An error occurred when attempting to create a module instance.");
                Console.WriteLine(ex.ToString());
                Environment.Exit(2);
            }
            modules.AddRange(amods);
        }
        return modules.AsReadOnly();
    }

    static IEnumerable<RegexbotModule> LoadModulesFromAssembly(Assembly asm, RegexbotClient k) {
        var eligibleTypes = from type in asm.GetTypes()
                            where !type.IsAssignableFrom(typeof(RegexbotModule))
                            where type.GetCustomAttribute<RegexbotModuleAttribute>() != null
                            select type;
        k._svcLogging.DoInstanceLog(false, LogName, $"Scanning {asm.GetName().Name}");

        var newmods = new List<RegexbotModule>();
        foreach (var t in eligibleTypes) {
            var mod = Activator.CreateInstance(t, k)!;
            k._svcLogging.DoInstanceLog(false, LogName,
                $"---> Loading module {t.FullName}");
            newmods.Add((RegexbotModule)mod);
        }
        return newmods;
    }
}
