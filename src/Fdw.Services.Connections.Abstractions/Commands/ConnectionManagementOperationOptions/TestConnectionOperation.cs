using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.Commands.ConnectionManagementOperationOptions;

/// <summary>
/// Test a specific connection.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionManagementOperations), "TestConnection", RestrictToCurrentCompilation = true)]
public sealed class TestConnectionOperation : ConnectionManagementOperationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestConnectionOperation"/> class.
    /// </summary>
    public TestConnectionOperation() : base(4, "TestConnection", modifiesState: false, requiresExistingConnection: true) { }
}
