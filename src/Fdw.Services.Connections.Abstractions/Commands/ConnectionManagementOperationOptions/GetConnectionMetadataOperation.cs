using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.Commands.ConnectionManagementOperationOptions;

/// <summary>
/// Get connection metadata.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionManagementOperations), "GetConnectionMetadata", RestrictToCurrentCompilation = true)]
public sealed class GetConnectionMetadataOperation : ConnectionManagementOperationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetConnectionMetadataOperation"/> class.
    /// </summary>
    public GetConnectionMetadataOperation() : base(2, "GetConnectionMetadata", modifiesState: false, requiresExistingConnection: true) { }
}
