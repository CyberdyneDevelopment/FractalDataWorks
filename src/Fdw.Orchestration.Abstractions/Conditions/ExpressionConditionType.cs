using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowConditionTypeOptions;

/// <summary>
/// Run based on custom expression.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowConditionTypes), "Expression", RestrictToCurrentCompilation = true)]
public sealed class ExpressionConditionType : WorkflowConditionTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionConditionType"/> class.
    /// </summary>
    public ExpressionConditionType() : base(3, "Expression", alwaysTrue: false, requiresExpression: true) { }
}
