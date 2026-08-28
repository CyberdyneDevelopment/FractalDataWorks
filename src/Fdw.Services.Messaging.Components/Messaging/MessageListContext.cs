using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.UI.Providers;
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
