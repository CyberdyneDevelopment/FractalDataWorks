using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.UI.Providers;
// Why: Fdw.Services.Messaging.MessagePayload (server-side) is accessible via
// ancestor-namespace lookup from this namespace, so `using X = SomeType` type aliases
// don't help (ancestor lookup beats using directives). A namespace alias creates a unique
// identifier that bypasses ancestor lookup — ClientModels.MessagePayload is unambiguous.
using ClientModels = Fdw.Services.Messaging.Clients.Models;

namespace Fdw.Services.Messaging.Components.Messaging;

public sealed class MessageListContext : ProviderContextBase
{
    public IReadOnlyList<ClientModels.MessagePayload> Messages { get; init; } = [];
    public int UnreadCount { get; init; }

    public Func<string?, string?, string?, int, int, Task> OnLoad { get; init; } = (_, _, _, _, _) => Task.CompletedTask;
    public Func<Task> OnMarkAllRead { get; init; } = () => Task.CompletedTask;
    public Func<Guid, Task> OnMarkRead { get; init; } = _ => Task.CompletedTask;
    public Func<Guid, Task> OnArchive { get; init; } = _ => Task.CompletedTask;
    public Func<Guid, Task> OnDismiss { get; init; } = _ => Task.CompletedTask;
}
