using Discord;

namespace RegexBot.Modules.RegexModerator;
/// <summary>
/// The namesake of RegexBot. This module allows users to define pattern-based rules with other constraints.
/// When triggered, one or more actions are executed as defined in its configuration.
/// </summary>
[RegexbotModule]
internal class RegexModerator : RegexbotModule {
    public RegexModerator(RegexbotClient bot) : base(bot) {
        DiscordClient.MessageReceived += DiscordClient_MessageReceived;
        DiscordClient.MessageUpdated += DiscordClient_MessageUpdated;
    }

    public override Task<object?> CreateGuildStateAsync(ulong guildID, JToken? config) {
        if (config == null) return Task.FromResult<object?>(null);
        var defs = new List<ConfDefinition>();

        if (config.Type != JTokenType.Array)
            throw new ModuleLoadException(Name + " configuration must be a JSON array.");

        // TODO better error reporting during this process
        foreach (var def in config.Children<JObject>())
            defs.Add(new ConfDefinition(def));

        if (defs.Count == 0) return Task.FromResult<object?>(null);
        Log(DiscordClient.GetGuild(guildID), $"Loaded {defs.Count} definition(s).");
        return Task.FromResult<object?>(defs.AsReadOnly());
    }

    private Task DiscordClient_MessageReceived(SocketMessage arg) => ReceiveIncomingMessage(arg);
    private Task DiscordClient_MessageUpdated(Cacheable<Discord.IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3) {
        // Ignore embed edits (see comment in MessageCachingSubservice)
        if (!arg2.EditedTimestamp.HasValue) return Task.CompletedTask;

        return ReceiveIncomingMessage(arg2);
    }

    /// <summary>
    /// Does initial message checking before further processing.
    /// </summary>
    private async Task ReceiveIncomingMessage(SocketMessage msg) {
        if (!Common.Utilities.IsValidUserMessage(msg, out var ch)) return;

        // Get config?
        var defs = GetGuildState<IEnumerable<ConfDefinition>>(ch.Guild.Id);
        if (defs == null) return;

        // Matching and response processing
        foreach (var item in defs) {
            // Need to check sender's moderator status here. Definition can't access mod list.
            var isMod = GetModerators(ch.Guild.Id).IsListMatch(msg, true);

            if (!item.IsMatch(msg, isMod)) continue;
            Log(ch.Guild, $"Rule '{item.Label}' triggered by {msg.Author}.");
            var exec = new ResponseExecutor(item, Bot, msg, (string logLine) => Log(ch.Guild, logLine));
            await exec.Execute();
        }
    }
}
