using System.Collections.Generic;
using System.Threading;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data.Limits;

/// <summary>
/// Default <see cref="IConnectionLimitResolver"/> that returns an empty list for all connections,
/// effectively disabling limit enforcement.
///
/// This resolver is registered by the ServiceTypeOption when no domain-specific resolver has
/// been registered. It is replaced at runtime by any IConnectionLimitResolver registered with
/// higher priority (e.g., the configuration-backed resolver in Services.Connections.MsSql).
/// </summary>
// Why: A no-op resolver keeps the LimitEnforcementDataGateway registered unconditionally
// in the DI stack. When limits ARE configured, callers replace this via
// services.AddSingleton<IConnectionLimitResolver, MyConfiguredResolver>().
internal sealed class PassThroughConnectionLimitResolver : IConnectionLimitResolver
{
    private static readonly IGenericResult<IReadOnlyList<ConnectionLimitConfiguration>> _empty =
        GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(
            (IReadOnlyList<ConnectionLimitConfiguration>)[]);

    /// <inheritdoc/>
    public IGenericResult<IReadOnlyList<ConnectionLimitConfiguration>> Resolve(
        string connectionName,
        CancellationToken cancellationToken = default)
        => _empty;
}
