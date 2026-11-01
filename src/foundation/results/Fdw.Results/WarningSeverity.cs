using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Warning severity - potential issues that don't prevent success.
/// </summary>
[TypeOption(typeof(ResultSeverities), "Warning", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WarningSeverity : ResultSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WarningSeverity"/> class.
    /// </summary>
    public WarningSeverity()
        : base(
            id: 3,
            name: "Warning",
            isSuccess: true,
            logLevelValue: 3, // LogLevel.Warning
            shouldLog: true,
            colorHint: "orange")
    {
    }
}
