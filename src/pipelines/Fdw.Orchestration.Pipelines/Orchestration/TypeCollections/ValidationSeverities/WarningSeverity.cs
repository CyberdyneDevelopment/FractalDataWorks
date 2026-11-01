using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions;
using ValidationSeveritiesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions.ValidationSeverities;

namespace Fdw.Orchestration.TypeCollections.ValidationSeverities;

/// <summary>
/// Warning validation severity that is logged but doesn't block execution.
/// </summary>
[TypeOption(typeof(ValidationSeveritiesCollection), "Warning", RestrictToCurrentCompilation = true)]
public sealed class WarningSeverity : ValidationSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WarningSeverity"/> class.
    /// </summary>
    public WarningSeverity()
        : base(
            id: 2,
            name: "Warning",
            level: 200,
            blocksExecution: false,
            requiresAcknowledgment: false,
            shouldLog: true)
    {
    }
}
