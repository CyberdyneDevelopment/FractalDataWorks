using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions.Commands.ConnectionManagementOperationOptions;

/// <summary>
/// Base class for connection management operations.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class ConnectionManagementOperationBase : TypeOptionBase<int, ConnectionManagementOperationBase>, IConnectionManagementOperation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionManagementOperationBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this connection management operation.</param>
    /// <param name="name">The name of this connection management operation.</param>
    /// <param name="modifiesState">Whether this operation modifies connection state.</param>
    /// <param name="requiresExistingConnection">Whether this operation requires an existing connection.</param>
    protected ConnectionManagementOperationBase(int id, string name, bool modifiesState, bool requiresExistingConnection)
        : base(id, name)
    {
        ModifiesState = modifiesState;
        RequiresExistingConnection = requiresExistingConnection;
    }

    /// <inheritdoc />
    public bool ModifiesState { get; }

    /// <inheritdoc />
    public bool RequiresExistingConnection { get; }
}
