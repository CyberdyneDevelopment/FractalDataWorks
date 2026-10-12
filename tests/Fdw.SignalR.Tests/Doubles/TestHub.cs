using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Fdw.SignalR.Tests.Doubles;

/// <summary>
/// Concrete <see cref="Fdw.SignalR.RealTimeHubBase{TClient}"/> subclass that exposes the protected surface
/// and lets a test override the <see cref="OnJoin"/>/<see cref="CanJoin"/> hooks.
/// </summary>
public class TestHub : RealTimeHubBase<ITestHubClient>
{
    /// <summary>Overrides the result of <see cref="CanJoin"/> when set.</summary>
    public Func<string, bool>? CanJoinOverride { get; set; }

    /// <summary>Overrides the behavior of <see cref="OnJoin"/> when set.</summary>
    public Func<Task>? OnJoinOverride { get; set; }

    /// <summary>Records whether <see cref="OnJoin"/> was invoked.</summary>
    public bool OnJoinInvoked { get; private set; }

    /// <inheritdoc/>
    protected override string HubName => "Test";

    /// <summary>Initializes a new instance of the <see cref="TestHub"/> class.</summary>
    /// <param name="logger">The logger, or <see langword="null"/> to exercise the NullLogger fallback.</param>
    public TestHub(ILogger<RealTimeHubBase<ITestHubClient>>? logger = null)
        : base(logger)
    {
    }

    /// <inheritdoc/>
    protected override Task OnJoin()
    {
        OnJoinInvoked = true;
        return OnJoinOverride?.Invoke() ?? Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override bool CanJoin(string scopeKey)
        => CanJoinOverride?.Invoke(scopeKey) ?? base.CanJoin(scopeKey);

    /// <summary>Exposes <see cref="RealTimeHubBase{TClient}.JoinScope"/> for testing.</summary>
    public Task JoinScopePublic(string scopeKey) => JoinScope(scopeKey);

    /// <summary>Exposes <see cref="RealTimeHubBase{TClient}.JoinAuthenticatedUserScope"/> for testing.</summary>
    public Task JoinAuthenticatedUserScopePublic() => JoinAuthenticatedUserScope();

    /// <summary>Exposes <see cref="RealTimeHubBase{TClient}.AuthenticatedUserId"/> for testing.</summary>
    public string? AuthenticatedUserIdPublic => AuthenticatedUserId;
}
