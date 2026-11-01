using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions.Commands.ConnectionManagementOperationOptions;

/// <summary>
/// Interface for connection management operations.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface IConnectionManagementOperation : ITypeOption<int, ConnectionManagementOperationBase>
{
    /// <summary>
    /// Gets a value indicating whether this operation modifies connection state.
    /// </summary>
    bool ModifiesState { get; }

    /// <summary>
    /// Gets a value indicating whether this operation requires an existing connection.
    /// </summary>
    bool RequiresExistingConnection { get; }
}
