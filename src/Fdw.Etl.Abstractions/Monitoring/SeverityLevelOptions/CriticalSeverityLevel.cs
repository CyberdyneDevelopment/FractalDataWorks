using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.SeverityLevelOptions;

/// <summary>
/// Critical error message.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SeverityLevels), "Critical", RestrictToCurrentCompilation = true)]
public sealed class CriticalSeverityLevel : SeverityLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CriticalSeverityLevel"/> class.
    /// </summary>
    public CriticalSeverityLevel() : base(4, "Critical", level: 4, logByDefault: true) { }
}
