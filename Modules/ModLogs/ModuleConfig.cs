using RegexBot.Common;

namespace RegexBot.Modules.ModLogs;
class ModuleConfig {
    public EntityName ReportingChannel { get; }

    public bool LogMessageDeletions { get; }
    public bool LogMessageEdits { get; }

    public ModuleConfig(JObject config) {
        const string RptChError = $"'{nameof(ReportingChannel)}' must be set to a valid channel name.";
        try {
            ReportingChannel = new EntityName(config[nameof(ReportingChannel)]?.Value<string>()!, EntityType.Channel);
        } catch (Exception) {
            throw new ModuleLoadException(RptChError);
        }

        // Individual logging settings - all default to false
        LogMessageDeletions = config[nameof(LogMessageDeletions)]?.Value<bool>() ?? false;
        LogMessageEdits = config[nameof(LogMessageEdits)]?.Value<bool>() ?? false;
    }
}