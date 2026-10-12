using System;
using Fdw.DevSession.Abstractions;
using Fdw.DevSession.Git;
using Fdw.DevSession.Sessions;
using Fdw.Mcp.Bus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fdw.DevSession;

/// <summary>DI registration for the development-session domain.</summary>
public static class DevSessionServiceCollectionExtensions
{
    /// <summary>
    /// Registers the worktree engine, session manager and workspace coordinator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything here is a SINGLETON on purpose. The session registry and the strand claim table
    /// are the coordination point — a scoped or transient manager would hand each caller its own
    /// empty registry, so two agents would never see each other's sessions and fencing would never
    /// detect a conflict. That failure is silent, which is why it is pinned here rather than left
    /// to a caller's lifetime choice.
    /// </para>
    /// <para>
    /// The MCP event bus is registered as a dependency rather than demanded from the caller,
    /// following the cascade rule that a domain registers the providers it needs. Both this and
    /// <c>AddMcpEventBus</c> use TryAdd semantics, so a host that has already configured its own
    /// bus keeps it.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDevSessions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMcpEventBus();

        services.TryAddSingleton<IGitRunner, GitProcessRunner>();
        services.TryAddSingleton<IWorktreeEngine, GitWorktreeEngine>();
        services.TryAddSingleton<IDevSessionManager, DevSessionManager>();
        services.TryAddSingleton<IWorkspaceCoordinator, WorkspaceCoordinator>();

        return services;
    }
}
