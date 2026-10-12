using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions;
using ValidationSeveritiesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions.ValidationSeverities;

namespace Fdw.Orchestration.TypeCollections.ValidationSeverities;

/// <summary>
/// Informational validation severity that doesn't affect execution.
/// </summary>
[TypeOption(typeof(ValidationSeveritiesCollection), "Info", RestrictToCurrentCompilation = true)]
public sealed class InfoSeverity : ValidationSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InfoSeverity"/> class.
    /// </summary>
    public InfoSeverity()
        : base(
            id: 1,
            name: "Info",
            level: 100,
            blocksExecution: false,
            requiresAcknowledgment: false,
            shouldLog: true)
    {
    }
}
