using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Trace severity - detailed diagnostic information.
/// </summary>
[TypeOption(typeof(ResultSeverities), "Trace", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TraceSeverity : ResultSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TraceSeverity"/> class.
    /// </summary>
    public TraceSeverity()
        : base(
            id: 0,
            name: "Trace",
            isSuccess: true,
            logLevelValue: 0, // LogLevel.Trace
            shouldLog: false,
            colorHint: "gray")
    {
    }
}
