using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.Commands.ConnectionManagementOperationOptions;

/// <summary>
/// List all available connections.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionManagementOperations), "ListConnections", RestrictToCurrentCompilation = true)]
public sealed class ListConnectionsOperation : ConnectionManagementOperationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListConnectionsOperation"/> class.
    /// </summary>
    public ListConnectionsOperation() : base(0, "ListConnections", modifiesState: false, requiresExistingConnection: false) { }
}
