using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions;
using ValidationSeveritiesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions.ValidationSeverities;

namespace Fdw.Orchestration.TypeCollections.ValidationSeverities;

/// <summary>
/// Error validation severity that may block execution depending on configuration.
/// </summary>
[TypeOption(typeof(ValidationSeveritiesCollection), "Error", RestrictToCurrentCompilation = true)]
public sealed class ErrorSeverity : ValidationSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorSeverity"/> class.
    /// </summary>
    public ErrorSeverity()
        : base(
            id: 3,
            name: "Error",
            level: 300,
            blocksExecution: true,
            requiresAcknowledgment: false,
            shouldLog: true)
    {
    }
}
