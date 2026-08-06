using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Execution;

/// <summary>
/// Test — a test-mode execution that runs with step-by-step pause/resume semantics.
/// Test executions pause at each stage boundary, allowing incremental approval.
/// </summary>
[TypeOption(typeof(ExecutionItemTypes), "Test", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TestItemType : ExecutionItemTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TestItemType"/> class.</summary>
    public TestItemType()
        : base(
            id: 11,
            name: "Test",
            displayName: "Test Execution",
            hierarchyLevel: 0,
            canHaveChildren: true,
            canHaveParent: false)
    {
    }
}
