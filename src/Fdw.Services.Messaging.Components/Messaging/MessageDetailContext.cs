using System;
using System.Threading.Tasks;
using Fdw.UI.Providers;
using ClientModels = Fdw.Services.Messaging.Clients.Models;

namespace Fdw.Services.Messaging.Components.Messaging;

public sealed class MessageDetailContext : ProviderContextBase
{
    public ClientModels.MessagePayload? Message { get; init; }

    public Func<Guid, Task> OnLoad { get; init; } = _ => Task.CompletedTask;
    public Func<Task> OnMarkRead { get; init; } = () => Task.CompletedTask;
    public Func<Task> OnArchive { get; init; } = () => Task.CompletedTask;
    public Func<Task> OnDismiss { get; init; } = () => Task.CompletedTask;
}
