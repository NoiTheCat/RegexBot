using RegexBot.Common;

namespace RegexBot.Modules.PendingOutRole;
class ModuleConfig {
    public EntityName Role { get; }

    public ModuleConfig(JObject conf) {
        var cfgRole = conf["Role"]?.Value<string>();
        if (string.IsNullOrWhiteSpace(cfgRole))
            throw new ModuleLoadException("Role was not specified.");
        Role = new EntityName(cfgRole);
        if (Role.Type != EntityType.Role)
            throw new ModuleLoadException("Name specified in configuration is not a role.");
    }
}
