using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Critical severity - system-level failure.
/// </summary>
[TypeOption(typeof(ResultSeverities), "Critical", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CriticalSeverity : ResultSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CriticalSeverity"/> class.
    /// </summary>
    public CriticalSeverity()
        : base(
            id: 5,
            name: "Critical",
            isSuccess: false,
            logLevelValue: 5, // LogLevel.Critical
            shouldLog: true,
            colorHint: "darkred")
    {
    }
}
