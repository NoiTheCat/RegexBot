using RegexBot.Common;

namespace RegexBot.Modules.PendingOutRole;
class ModuleConfig {
    public EntityName Role { get; }

    public ModuleConfig(JObject conf) {
        try {
            Role = new EntityName(conf[nameof(Role)]?.Value<string>()!, EntityType.Role);
        } catch (ArgumentException) {
                throw new ModuleLoadException("Role was not properly specified.");
        } catch (FormatException) {
                throw new ModuleLoadException("Name specified in configuration is not a role.");
        }
    }
}
