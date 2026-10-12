using Fdw.DevSession.Abstractions;
using Fdw.Mcp.Bus.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.DevSession.Tests;

/// <summary>That AddDevSessions produces a container the domain can actually run from.</summary>
public sealed class DevSessionRegistrationTests
{
    [Fact]
    public void Every_contract_resolves()
    {
        using var provider = new ServiceCollection().AddLogging().AddDevSessions().BuildServiceProvider();

        provider.GetRequiredService<IWorktreeEngine>().ShouldNotBeNull();
        provider.GetRequiredService<IDevSessionManager>().ShouldNotBeNull();
        provider.GetRequiredService<IWorkspaceCoordinator>().ShouldNotBeNull();
    }

    [Fact]
    public void The_event_bus_is_registered_as_a_dependency()
    {
        using var provider = new ServiceCollection().AddLogging().AddDevSessions().BuildServiceProvider();

        provider.GetRequiredService<IMcpEventBus>().ShouldNotBeNull();
    }

    [Fact]
    public void The_session_manager_and_coordinator_are_shared_singletons()
    {
        using var provider = new ServiceCollection().AddLogging().AddDevSessions().BuildServiceProvider();

        provider.GetRequiredService<IDevSessionManager>()
            .ShouldBeSameAs(provider.GetRequiredService<IDevSessionManager>());
        provider.GetRequiredService<IWorkspaceCoordinator>()
            .ShouldBeSameAs(provider.GetRequiredService<IWorkspaceCoordinator>());
    }

    [Fact]
    public void A_host_supplied_engine_is_not_overwritten()
    {
        var replacement = new ThrowingWorktreeEngine();
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IWorktreeEngine>(replacement)
            .AddDevSessions()
            .BuildServiceProvider();

        provider.GetRequiredService<IWorktreeEngine>().ShouldBeSameAs(replacement);
    }
}
