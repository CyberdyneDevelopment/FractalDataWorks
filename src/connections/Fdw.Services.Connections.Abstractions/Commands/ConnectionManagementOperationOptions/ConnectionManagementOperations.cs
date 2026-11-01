using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.Commands.ConnectionManagementOperationOptions;

/// <summary>
/// TypeCollection for connection management operations.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for connection management operations.
/// Source generator creates static properties for each registered connection management operation.
/// </remarks>
[TypeCollection(typeof(ConnectionManagementOperationBase), typeof(IConnectionManagementOperation), typeof(ConnectionManagementOperations))]
public sealed partial class ConnectionManagementOperations : TypeCollectionBase<ConnectionManagementOperationBase, IConnectionManagementOperation>
{
}
