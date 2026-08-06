using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.Commands.ConnectionManagementOperationOptions;

/// <summary>
/// Refresh connection status.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionManagementOperations), "RefreshConnectionStatus", RestrictToCurrentCompilation = true)]
public sealed class RefreshConnectionStatusOperation : ConnectionManagementOperationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshConnectionStatusOperation"/> class.
    /// </summary>
    public RefreshConnectionStatusOperation() : base(3, "RefreshConnectionStatus", modifiesState: false, requiresExistingConnection: true) { }
}
