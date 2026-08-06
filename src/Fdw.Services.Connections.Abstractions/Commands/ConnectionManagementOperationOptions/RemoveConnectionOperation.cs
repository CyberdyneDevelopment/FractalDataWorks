using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.Commands.ConnectionManagementOperationOptions;

/// <summary>
/// Remove a specific connection.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionManagementOperations), "RemoveConnection", RestrictToCurrentCompilation = true)]
public sealed class RemoveConnectionOperation : ConnectionManagementOperationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveConnectionOperation"/> class.
    /// </summary>
    public RemoveConnectionOperation() : base(1, "RemoveConnection", modifiesState: true, requiresExistingConnection: true) { }
}
