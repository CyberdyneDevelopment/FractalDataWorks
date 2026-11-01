using Fdw.Services.Abstractions.Health.Monitoring;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// Builds the in-process health monitor.
/// </summary>
/// <remarks>
/// This implementation resolves nothing, so it completes synchronously. That is not the fake
/// asynchrony the factories were carrying: a provider's contract is asynchronous because RESOLVING
/// may be — fetching a secret, opening a handle — and an implementation with nothing to fetch simply
/// has nothing to await. A factory had no such excuse, which is why theirs came off.
/// </remarks>
public sealed class LocalHealthMonitorProvider
    : ImplementationServiceProviderBase<IHealthMonitorService, IHealthMonitorImplementationConfiguration>,
      ILocalHealthMonitorProvider
{
    private readonly ILocalHealthMonitorFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalHealthMonitorProvider"/> class.
    /// </summary>
    /// <param name="factory">Builds the monitor.</param>
    public LocalHealthMonitorProvider(ILocalHealthMonitorFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<IHealthMonitorService>> Create(
        IHealthMonitorImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_factory.Create(configuration));
}
