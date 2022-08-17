using RegexBot.Services.SharedEventService;

namespace RegexBot;
partial class RegexbotClient {
    private readonly SharedEventService _svcSharedEvents;

    /// <summary>
    /// Delegate used for the <seealso cref="SharedEventReceived"/> event.
    /// </summary>
    /// <param name="ev">The incoming event instance.</param>
    public delegate Task IncomingSharedEventHandler(ISharedEvent ev);

    /// <summary>
    /// Sends an object instance implementing <seealso cref="ISharedEvent"/> to all modules and services
    /// subscribed to the <seealso cref="SharedEventReceived"/> event.
    /// </summary>
    /// <remarks>
    /// This method is non-blocking. Event handlers are executed in their own thread.
    /// </remarks>
    public Task PushSharedEventAsync(ISharedEvent ev) => _svcSharedEvents.PushSharedEventAsync(ev);

    /// <summary>
    /// This event is fired after a module or internal service calls <see cref="PushSharedEventAsync"/>.
    /// </summary>
    /// <remarks>
    /// Subscribers to this event are handled on a "fire and forget" basis and may execute on a thread
    /// separate from the main one handling Discord events. Ensure that the code executed by the handler
    /// executes quickly, is thread-safe, and throws no exceptions.
    /// </remarks>
    public event IncomingSharedEventHandler? SharedEventReceived {
        add { lock (_svcSharedEvents) _svcSharedEvents.Subscribers += value; }
        remove { lock (_svcSharedEvents) _svcSharedEvents.Subscribers -= value; }
    }
}