using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// A transaction over one of the recording gateways these fixtures use, so a save under test writes
/// where the fixture is already watching.
/// </summary>
/// <remarks>
/// Configuration saves run in a unit of work now, so a double that refuses to begin one fails every
/// save before it writes anything. Delegating to the gateway keeps the fixtures' existing assertions
/// about recorded commands true, and records whether the unit was committed or abandoned — which is
/// what a test about a refused write has to assert now that nothing is written to compensate.
/// </remarks>
internal sealed class RecordingTransaction : IDataGatewayTransaction
{
    private readonly IConfigurationGateway _gateway;

    internal RecordingTransaction(IConfigurationGateway gateway) => _gateway = gateway;

    /// <summary>Gets a value indicating whether the unit was committed.</summary>
    internal bool Committed { get; private set; }

    /// <summary>Gets a value indicating whether the unit was rolled back.</summary>
    internal bool RolledBack { get; private set; }

    /// <inheritdoc/>
    public bool IsActive => !Committed && !RolledBack;

    /// <inheritdoc/>
    public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        => _gateway.Execute<T>(command, target, cancellationToken);

    /// <inheritdoc/>
    public Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        => _gateway.Execute(command, target, cancellationToken);

    /// <inheritdoc/>
    public Task<IGenericResult> Commit(CancellationToken cancellationToken = default)
    {
        Committed = true;
        return Task.FromResult(GenericResult.Success());
    }

    /// <inheritdoc/>
    public Task<IGenericResult> Rollback(CancellationToken cancellationToken = default)
    {
        RolledBack = true;
        return Task.FromResult(GenericResult.Success());
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
