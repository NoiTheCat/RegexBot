using RegexBot.Common;

namespace RegexBot.Modules.ModCommands.Commands;
class RoleAdd : RoleManipulation {
    protected override (string, string) String1 => ("Adds", "to");
    protected override string String2 => "set";
    public RoleAdd(ModCommands module, JObject config) : base(module, config) { }
    protected override async Task ContinueInvoke(SocketGuildUser target, SocketRole role) => await target.AddRoleAsync(role);
}

class RoleDel : RoleManipulation {
    protected override (string, string) String1 => ("Removes", "from");
    protected override string String2 => "unset";
    public RoleDel(ModCommands module, JObject config) : base(module, config) { }
    protected override async Task ContinueInvoke(SocketGuildUser target, SocketRole role) => await target.RemoveRoleAsync(role);
}

// Role adding and removing is largely the same, and thus are handled in a single class.
abstract class RoleManipulation : CommandConfig {
    private readonly string _usage;

    protected EntityName Role { get; }
    protected string? SuccessMessage { get; }
    protected override string DefaultUsageMsg => _usage;
    protected abstract (string, string) String1 { get; }
    protected abstract string String2 { get; }

    // Configuration:
    // "role" - string; The given role that applies to this command.
    // "successmsg" - string; Messages to display on command success. Overrides default.
    protected RoleManipulation(ModCommands module, JObject config) : base(module, config) {
        try {
            Role = new EntityName(config[nameof(Role)]?.Value<string>()!, EntityType.Role);
        } catch (ArgumentNullException) {
            throw new ModuleLoadException($"'{nameof(Role)}' must be provided.");
        } catch (FormatException) {
            throw new ModuleLoadException($"The value in '{nameof(Role)}' is not a role.");
        }
        
        SuccessMessage = config[nameof(SuccessMessage)]?.Value<string>();

        _usage = $"{Command} `user or user ID`\n" +
            string.Format("{0} the '{1}' role {2} the specified user.",
            String1.Item1, Role.Name ?? Role.Id.ToString(), String1.Item2);
    }

    public override async Task Invoke(SocketGuild g, SocketMessage msg) {
        // TODO reason in further parameters?
        var line = msg.Content.Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
        string targetstr;
        if (line.Length < 2) {
            await SendUsageMessageAsync(msg.Channel, null);
            return;
        }
        targetstr = line[1];

        // Retrieve targets
        var targetQuery = Module.Bot.EcQueryGuildUser(g.Id, targetstr);
        var targetUser = targetQuery != null ? g.GetUser((ulong)targetQuery.UserId) : null;
        if (targetUser == null) {
            await SendUsageMessageAsync(msg.Channel, TargetNotFound);
            return;
        }
        var targetRole = Role.FindRoleIn(g, true);
        if (targetRole == null) {
            await SendUsageMessageAsync(msg.Channel, ":x: **Failed to determine the specified role for this command.**");
            return;
        }

        // Do the specific thing and report back
        await ContinueInvoke(targetUser, targetRole);
        const string defaultmsg = ":white_check_mark: Successfully {0} role for **$target**.";
        var success = SuccessMessage ?? string.Format(defaultmsg, String2);
        success = success.Replace("$target", targetUser.Nickname ?? targetUser.Username);
        await msg.Channel.SendMessageAsync(success);
    }

    protected abstract Task ContinueInvoke(SocketGuildUser target, SocketRole role);
}