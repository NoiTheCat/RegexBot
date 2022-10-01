using RegexBot.Modules.ModCommands.Commands;
using System.Collections.ObjectModel;
using System.Reflection;

namespace RegexBot.Modules.ModCommands;
class ModuleConfig {
    public ReadOnlyDictionary<string, CommandConfig> Commands { get; }

    public ModuleConfig(ModCommands instance, JToken conf) {
        if (conf.Type != JTokenType.Array)
            throw new ModuleLoadException("Command definitions must be defined as objects in a JSON array.");

        // Command instance creation
        var commands = new Dictionary<string, CommandConfig>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in conf.Children<JObject>()) {
            string Label;
            Label = def[nameof(Label)]?.Value<string>()
                ?? throw new ModuleLoadException($"'{nameof(Label)}' was not defined in a command definition.");
            var cmd = CreateCommandInstance(instance, def);
            if (commands.ContainsKey(cmd.Command)) {
                throw new ModuleLoadException(
                    $"{Label}: The command name '{cmd.Command}' is already in use by '{commands[cmd.Command].Label}'.");
            }
            commands.Add(cmd.Command, cmd);
        }
        Commands = new ReadOnlyDictionary<string, CommandConfig>(commands);
    }

    private static readonly ReadOnlyDictionary<string, Type> _commandTypes = new(
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase) {
            { "ban",        typeof(Ban) },
            { "confreload", typeof(ConfReload) },
            { "kick",       typeof(Kick) },
            { "say",        typeof(Say) },
            { "unban",      typeof(Unban) },
            { "note",       typeof(Note) },
            { "addnote",    typeof(Note) },
            { "warn",       typeof(Warn) },
            { "timeout",    typeof(Commands.Timeout) },
            { "untimeout",  typeof(Untimeout)},
            { "addrole",    typeof(RoleAdd) },
            { "roleadd",    typeof(RoleAdd) },
            { "delrole",    typeof(RoleDel) },
            { "roledel",    typeof(RoleDel) }
        }
    );

    private static CommandConfig CreateCommandInstance(ModCommands instance, JObject def) {
        var label = def[nameof(CommandConfig.Label)]?.Value<string>()!;
        
        var command = def[nameof(CommandConfig.Command)]?.Value<string>();
        if (string.IsNullOrWhiteSpace(command))
            throw new ModuleLoadException($"{label}: '{nameof(CommandConfig.Command)}' was not specified.");
        if (command.Contains(' '))
            throw new ModuleLoadException($"{label}: '{nameof(CommandConfig.Command)}' must not contain spaces.");

        string? Type;
        Type = def[nameof(Type)]?.Value<string>();
        if (string.IsNullOrWhiteSpace(Type))
            throw new ModuleLoadException($"'{nameof(Type)}' must be specified within definition for '{label}'.");
        if (!_commandTypes.TryGetValue(Type, out Type? cmdType)) {
            throw new ModuleLoadException($"{label}: '{nameof(Type)}' does not have a valid value.");
        } else {
            try {
                return (CommandConfig)Activator.CreateInstance(cmdType, instance, def)!;
            } catch (TargetInvocationException ex) when (ex.InnerException is ModuleLoadException) {
                throw new ModuleLoadException($"{label}: {ex.InnerException.Message}");
            }
        }
    }
}
