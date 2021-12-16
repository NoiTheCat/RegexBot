using Newtonsoft.Json.Linq;
using Noikoio.RegexBot.ConfigItem;

namespace Noikoio.RegexBot.Module.PendingAutoRole
{
    class ModuleConfig
    {
        public EntityName Role { get; }

        public ModuleConfig(JObject conf)
        {
            var cfgRole = conf["Role"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(cfgRole))
                throw new RuleImportException("Role was not specified.");
            Role = new EntityName(cfgRole, EntityType.Role);
        }
    }
}
