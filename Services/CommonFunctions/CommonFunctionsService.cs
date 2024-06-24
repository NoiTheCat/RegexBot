using RegexBot.Services.CommonFunctions;

namespace RegexBot.Services.CommonFunctions {
    /// <summary>
    /// Implements certain common actions that modules may want to perform. Using this service to perform those
    /// functions may help enforce a sense of consistency across modules when performing common actions, and may
    /// inform services which provide any additional features the ability to respond to those actions ahead of time.
    /// </summary>
    internal partial class CommonFunctionsService(RegexbotClient bot) : Service(bot) {
        // Note: Several classes within this service created by its hooks are meant to be sent to modules,
        // therefore those public classes are placed into the root RegexBot namespace for the developer's convenience.
    }
}

namespace RegexBot {
    partial class RegexbotClient {
        private readonly CommonFunctionsService _svcCommonFunctions;
    }
}