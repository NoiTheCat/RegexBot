using System.Threading.Channels;

namespace RegexBot.Services.SharedEventService;
/// <summary>
/// Implements a queue which any service or module may send objects into,
/// which are then sent to subscribing services and/or modules. Allows for simple,
/// basic sharing of information between separate parts of the program.
/// </summary>
class SharedEventService : Service {
    private readonly Channel<ISharedEvent> _items;
    //private readonly Task _itemPropagationWorker;

    internal SharedEventService(RegexbotClient bot) : base(bot) {
        _items = Channel.CreateUnbounded<ISharedEvent>();
        _ = Task.Factory.StartNew(ItemPropagator, CancellationToken.None,
                                  TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    // Hooked (lock this on self)
    internal event RegexbotClient.IncomingSharedEventHandler? Subscribers;

    internal async Task PushSharedEventAsync(ISharedEvent ev) {
        await _items.Writer.WriteAsync(ev);
    }

    private async Task ItemPropagator() {
        while (true) {
            var ev = await _items.Reader.ReadAsync();

            Delegate[]? subscribed;
            lock (this) {
                subscribed = Subscribers?.GetInvocationList();
                if (subscribed == null || subscribed.Length == 0) return;
            }

            foreach (var handler in subscribed) {
                // Fire and forget!
                _ = Task.Run(async () => {
                    try {
                        await (Task)handler.DynamicInvoke(ev)!;
                    } catch (Exception ex) {
                        Log("Unhandled exception in shared event handler:" + ex.ToString());
                    }
                });
            }
        }
    }
}
