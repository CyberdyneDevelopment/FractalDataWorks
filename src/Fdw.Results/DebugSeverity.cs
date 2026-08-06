using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Debug severity - debugging information.
/// </summary>
[TypeOption(typeof(ResultSeverities), "Debug", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DebugSeverity : ResultSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DebugSeverity"/> class.
    /// </summary>
    public DebugSeverity()
        : base(
            id: 1,
            name: "Debug",
            isSuccess: true,
            logLevelValue: 1, // LogLevel.Debug
            shouldLog: false,
            colorHint: "lightgray")
    {
    }
}
