using Discord.WebSocket;
using System.Reflection;

namespace RegexBot;

/// <summary>
/// The RegexBot client instance. 
/// </summary>
public partial class RegexbotClient {
    /// <summary>
    /// Gets application instance configuration.
    /// </summary>
    internal InstanceConfig Config { get; }

    /// <summary>
    /// Gets the Discord client instance.
    /// </summary>
    public DiscordSocketClient DiscordClient { get; }

    /// <summary>
    /// Gets all loaded modules in an iterable form.
    /// </summary>
    internal IReadOnlyCollection<RegexbotModule> Modules { get; }

    internal RegexbotClient(InstanceConfig conf, DiscordSocketClient client) {
        Config = conf;
        DiscordClient = client;

        // Get all services started up
        _svcLogging = new Services.Logging.LoggingService(this);
        _svcGuildState = new Services.ModuleState.ModuleStateService(this);
        _svcCommonFunctions = new Services.CommonFunctions.CommonFunctionsService(this);
        _svcEntityCache = new Services.EntityCache.EntityCacheService(this);

        // Load externally defined functionality
        Modules = ModuleLoader.Load(Config, this);

        // Everything's ready to go. Print the welcome message here.
        var ver = Assembly.GetExecutingAssembly().GetName().Version!.ToString(3);
        _svcLogging.DoInstanceLog(false, nameof(RegexBot), $"{nameof(RegexBot)} v{ver}. https://github.com/NoiTheCat/RegexBot");
    }
}
