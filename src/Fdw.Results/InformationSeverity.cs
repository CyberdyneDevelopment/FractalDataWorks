using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Information severity - general operational information.
/// </summary>
[TypeOption(typeof(ResultSeverities), "Information", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InformationSeverity : ResultSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InformationSeverity"/> class.
    /// </summary>
    public InformationSeverity()
        : base(
            id: 2,
            name: "Information",
            isSuccess: true,
            logLevelValue: 2, // LogLevel.Information
            shouldLog: true,
            colorHint: "blue")
    {
    }
}
