using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Moq;

namespace Fdw.Tests.Shared;

/// <summary>
/// A transaction over a mocked configuration gateway, so a save under test writes where the fixture
/// is already watching.
/// </summary>
/// <remarks>
/// Configuration saves run in a unit of work, so a gateway double that cannot begin one fails every
/// save before it writes anything — a Moq gateway with no BeginTransaction setup returns a null task
/// and the save throws inside it. Forwarding to the gateway keeps each fixture's existing assertions
/// about recorded commands true, and records whether the unit was committed or abandoned.
///
/// One file, linked into each test project that needs it rather than copied, so the doubles cannot
/// drift apart. It lives outside any project so adding it costs a Compile Include, not a reference.
/// </remarks>
internal sealed class GatewayWriteTransaction : IDataGatewayTransaction
{
    private readonly IConfigurationGateway _gateway;

    internal GatewayWriteTransaction(IConfigurationGateway gateway) => _gateway = gateway;

    /// <summary>Gets a value indicating whether the unit was committed.</summary>
    internal bool Committed { get; private set; }

    /// <summary>Gets a value indicating whether the unit was abandoned.</summary>
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

    /// <summary>Gives a mocked gateway a unit of work that writes through the mock itself.</summary>
    /// <param name="gateway">The gateway double a fixture is asserting against.</param>
    /// <returns>The transaction the gateway will hand out, for asserting commit or rollback.</returns>
    internal static GatewayWriteTransaction StubOn(Mock<IConfigurationGateway> gateway)
    {
        var transaction = new GatewayWriteTransaction(gateway.Object);
        gateway
            .Setup(g => g.BeginTransaction(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<IDataGatewayTransaction>.Success(transaction));
        return transaction;
    }
}
