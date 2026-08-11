using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.UI.Providers;
// Why: Fdw.Services.Messaging.AccessRequestPayload (server-side) is accessible via
// ancestor-namespace lookup from this namespace. Namespace alias bypasses ancestor lookup.
using ClientModels = Fdw.Services.Messaging.Clients.Models;

namespace Fdw.Services.Messaging.Components.Messaging;

public sealed class AccessRequestListContext : ProviderContextBase
{
    public IReadOnlyList<ClientModels.AccessRequestPayload> AccessRequests { get; init; } = [];

    public Func<Task> OnLoad { get; init; } = () => Task.CompletedTask;
    public Func<Guid, string?, Task> OnApprove { get; init; } = (_, _) => Task.CompletedTask;
    public Func<Guid, string?, Task> OnDeny { get; init; } = (_, _) => Task.CompletedTask;
    public Func<ClientModels.CreateAccessRequestModel, Task> OnCreate { get; init; } = _ => Task.CompletedTask;
}
