using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace RegexBot;
static class ModuleLoader {
    /// <summary>
    /// Given the instance configuration, loads all appropriate types from file specified in it.
    /// </summary>
    internal static ReadOnlyCollection<RegexbotModule> Load(InstanceConfig conf, RegexbotClient rb) {
        var path = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location) + Path.DirectorySeparatorChar;
        var modules = new List<RegexbotModule>();

        // Load self, then others if defined
        modules.AddRange(LoadModulesFromAssembly(Assembly.GetExecutingAssembly(), rb));

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
                amods = LoadModulesFromAssembly(a, rb);
            } catch (Exception ex) {
                Console.WriteLine("An error occurred when attempting to create a module instance.");
                Console.WriteLine(ex.ToString());
                Environment.Exit(2);
            }
            modules.AddRange(amods);
        }
        return modules.AsReadOnly();
    }

    static IEnumerable<RegexbotModule> LoadModulesFromAssembly(Assembly asm, RegexbotClient rb) {
        var eligibleTypes = from type in asm.GetTypes()
                            where !type.IsAssignableFrom(typeof(RegexbotModule))
                            where type.GetCustomAttribute<RegexbotModuleAttribute>() != null
                            select type;

        var newreport = new StringBuilder($"---> Modules in {asm.GetName().Name}:");
        var newmods = new List<RegexbotModule>();
        foreach (var t in eligibleTypes) {
            var mod = Activator.CreateInstance(t, rb)!;
            newreport.Append($" {t.Name}");
            newmods.Add((RegexbotModule)mod);
        }
        rb._svcLogging.DoLog(false, nameof(ModuleLoader), newreport.ToString());
        return newmods;
    }
}
