using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Error severity - operation failed.
/// </summary>
[TypeOption(typeof(ResultSeverities), "Error", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ErrorSeverity : ResultSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorSeverity"/> class.
    /// </summary>
    public ErrorSeverity()
        : base(
            id: 4,
            name: "Error",
            isSuccess: false,
            logLevelValue: 4, // LogLevel.Error
            shouldLog: true,
            colorHint: "red")
    {
    }
}
