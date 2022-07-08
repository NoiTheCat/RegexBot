using RegexBot.Common;

namespace RegexBot.Modules.ModLogs;
class ModuleConfig {
    public EntityName ReportingChannel { get; }

    public bool LogMessageDeletions { get; }
    public bool LogMessageEdits { get; }

    public ModuleConfig(JObject config) {
        const string RptChError = $"'{nameof(ReportingChannel)}' must be set to a valid channel name.";
        var rptch = config[nameof(ReportingChannel)]?.Value<string>();
        if (string.IsNullOrWhiteSpace(rptch)) throw new ModuleLoadException(RptChError);
        ReportingChannel = new EntityName(rptch);
        if (ReportingChannel.Type != EntityType.Channel) throw new ModuleLoadException(RptChError);

        // Individual logging settings - all default to false
        LogMessageDeletions = config[nameof(LogMessageDeletions)]?.Value<bool>() ?? false;
        LogMessageEdits = config[nameof(LogMessageEdits)]?.Value<bool>() ?? false;
    }
}