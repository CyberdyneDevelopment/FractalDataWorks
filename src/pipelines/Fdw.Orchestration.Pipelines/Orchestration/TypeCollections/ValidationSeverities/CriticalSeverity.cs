using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions;
using ValidationSeveritiesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions.ValidationSeverities;

namespace Fdw.Orchestration.TypeCollections.ValidationSeverities;

/// <summary>
/// Critical validation severity that always blocks execution.
/// </summary>
[TypeOption(typeof(ValidationSeveritiesCollection), "Critical", RestrictToCurrentCompilation = true)]
public sealed class CriticalSeverity : ValidationSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CriticalSeverity"/> class.
    /// </summary>
    public CriticalSeverity()
        : base(
            id: 4,
            name: "Critical",
            level: 400,
            blocksExecution: true,
            requiresAcknowledgment: true,
            shouldLog: true)
    {
    }
}
