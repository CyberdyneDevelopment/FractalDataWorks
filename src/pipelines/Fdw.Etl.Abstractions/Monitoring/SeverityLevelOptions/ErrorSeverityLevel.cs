using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.SeverityLevelOptions;

/// <summary>
/// Error message.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SeverityLevels), "Error", RestrictToCurrentCompilation = true)]
public sealed class ErrorSeverityLevel : SeverityLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorSeverityLevel"/> class.
    /// </summary>
    public ErrorSeverityLevel() : base(3, "Error", level: 3, logByDefault: true) { }
}
